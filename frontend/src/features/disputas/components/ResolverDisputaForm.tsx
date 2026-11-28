import { useMemo, useState } from 'react'
import { zodResolver } from '@hookform/resolvers/zod'
import { useForm, useWatch, type Resolver } from 'react-hook-form'
import { aplicarErroresAlFormulario, extraerMensajeErrorBackend, leerErrorDto } from '../../../shared/api/extraerMensajeErrorBackend'
import { Boton } from '../../../shared/components/Boton'
import { AlertaFormulario, CampoAreaTexto } from '../../../shared/components/Formulario'
import type { EvaluacionDto, SegmentoDto } from '../../llamadas/types/llamada.types'
import { CamposAjusteCriterio } from '../../revision/components/CamposAjusteCriterio'
import { esConflictoDeRevision } from '../../revision/utils/conflicto'
import { resolverDisputa } from '../api/disputasApi'
import { crearResolverDisputaSchema, type ResolverDisputaFormulario } from '../schemas/disputa.schema'
import { DecisionDisputaCampo } from './DecisionDisputaCampo'

const CAMPOS = ['decision', 'resolucion', 'criterios'] as const

interface PropsResolver {
  disputaId: string
  evaluacion: EvaluacionDto
  segmentos: SegmentoDto[]
  onResuelta: () => void
  onConflicto: (mensaje: string) => void
}

/** Mantener (las notas no cambian) o Modificar (ajuste completo, mismas reglas que la revisión humana). */
export function ResolverDisputaForm({ disputaId, evaluacion, segmentos, onResuelta, onConflicto }: PropsResolver) {
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null)
  const esquema = useMemo(() => crearResolverDisputaSchema(new Map(segmentos.map((s) => [s.id, s.textoRedactado]))), [segmentos])
  const {
    control,
    register,
    handleSubmit,
    setError,
    formState: { errors, isValid, isSubmitting },
  } = useForm<ResolverDisputaFormulario>({
    // La unión valida según la decisión; los valores del formulario siempre conservan los criterios.
    resolver: zodResolver(esquema) as unknown as Resolver<ResolverDisputaFormulario>,
    mode: 'onChange',
    defaultValues: {
      decision: 'Mantener',
      resolucion: '',
      criterios: evaluacion.criterios.map((c) => ({
        criterioId: c.criterioId,
        puntaje: c.puntajeVigente,
        justificacion: c.justificacion,
        evidencias: c.evidencias.map((e) => ({ segmentoId: e.segmentoId, citaTextual: e.citaTextual })),
      })),
    },
  })
  const decision = useWatch({ control, name: 'decision' })

  const enviar = async (datos: ResolverDisputaFormulario) => {
    setErrorGeneral(null)
    try {
      await resolverDisputa(disputaId, {
        decision: datos.decision,
        resolucion: datos.resolucion,
        revisionEsperada: evaluacion.revision,
        ...(datos.decision === 'Modificar' ? { criterios: datos.criterios } : {}),
      })
      onResuelta()
    } catch (error) {
      if (esConflictoDeRevision(error) || leerErrorDto(error)?.codigo === 'DISPUTA_YA_RESUELTA') onConflicto(extraerMensajeErrorBackend(error))
      else setErrorGeneral(aplicarErroresAlFormulario(error, setError, CAMPOS))
    }
  }

  return (
    <form onSubmit={handleSubmit(enviar)} noValidate className="flex flex-col gap-6">
      <AlertaFormulario mensaje={errorGeneral} />
      <DecisionDisputaCampo register={register} error={errors.decision?.message} />
      <CampoAreaTexto
        etiqueta="Resolución"
        rows={4}
        ayuda="Explica la decisión al agente. Entre 10 y 2000 caracteres."
        error={errors.resolucion?.message}
        {...register('resolucion')}
      />
      {decision === 'Modificar' && (
        <div className="flex flex-col gap-4">
          <p className="text-sm text-texto-secundario">Ajusta los {evaluacion.criterios.length} criterios. Los cambios quedan como revisión del supervisor.</p>
          {evaluacion.criterios.map((criterio, indice) => (
            <CamposAjusteCriterio key={criterio.criterioId} indice={indice} criterio={criterio} control={control} register={register} errors={errors} segmentos={segmentos} />
          ))}
        </div>
      )}
      <div className="flex justify-end border-t border-borde pt-5">
        <Boton type="submit" cargando={isSubmitting} disabled={!isValid}>
          {decision === 'Modificar' ? 'Modificar notas y resolver' : 'Mantener notas y resolver'}
        </Boton>
      </div>
    </form>
  )
}
