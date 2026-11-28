import { useMemo, useState } from 'react'
import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { aplicarErroresAlFormulario, extraerMensajeErrorBackend } from '../../../shared/api/extraerMensajeErrorBackend'
import { Boton } from '../../../shared/components/Boton'
import { AlertaFormulario, CampoAreaTexto } from '../../../shared/components/Formulario'
import type { EvaluacionDto, SegmentoDto } from '../../llamadas/types/llamada.types'
import { ajustarEvaluacion } from '../api/revisionApi'
import { crearAjusteSchema, type AjusteFormulario } from '../schemas/ajuste.schema'
import { esConflictoDeRevision } from '../utils/conflicto'
import { CamposAjusteCriterio } from './CamposAjusteCriterio'

const CAMPOS = ['motivo', 'criterios'] as const

interface PropsAjuste {
  evaluacion: EvaluacionDto
  segmentos: SegmentoDto[]
  onAjustada: () => void
  onCancelar: () => void
  onConflicto: (mensaje: string) => void
}

/** Valores iniciales: los vigentes de cada criterio (puntaje, justificación y evidencias). */
function valoresIniciales(evaluacion: EvaluacionDto): AjusteFormulario {
  return {
    motivo: '',
    criterios: evaluacion.criterios.map((c) => ({
      criterioId: c.criterioId,
      puntaje: c.puntajeVigente,
      justificacion: c.justificacion,
      evidencias: c.evidencias.map((e) => ({ segmentoId: e.segmentoId, citaTextual: e.citaTextual })),
    })),
  }
}

/** Ajuste completo del supervisor: motivo y todos los criterios de la rúbrica. El resultado de la IA se conserva. */
export function AjusteEvaluacionForm({ evaluacion, segmentos, onAjustada, onCancelar, onConflicto }: PropsAjuste) {
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null)
  const esquema = useMemo(() => crearAjusteSchema(new Map(segmentos.map((s) => [s.id, s.textoRedactado]))), [segmentos])
  const {
    control,
    register,
    handleSubmit,
    setError,
    formState: { errors, isValid, isSubmitting },
  } = useForm<AjusteFormulario>({ resolver: zodResolver(esquema), mode: 'onChange', defaultValues: valoresIniciales(evaluacion) })

  const enviar = async (datos: AjusteFormulario) => {
    setErrorGeneral(null)
    try {
      await ajustarEvaluacion(evaluacion.id, { ...datos, revisionEsperada: evaluacion.revision })
      onAjustada()
    } catch (error) {
      if (esConflictoDeRevision(error)) onConflicto(extraerMensajeErrorBackend(error))
      else setErrorGeneral(aplicarErroresAlFormulario(error, setError, CAMPOS))
    }
  }

  return (
    <form onSubmit={handleSubmit(enviar)} noValidate className="flex flex-col gap-6">
      <AlertaFormulario mensaje={errorGeneral} />
      <CampoAreaTexto
        etiqueta="Motivo del ajuste"
        rows={3}
        ayuda="Queda en el historial de la evaluación. Entre 10 y 2,000 caracteres."
        error={errors.motivo?.message}
        {...register('motivo')}
      />
      {(errors.criterios?.root?.message ?? errors.criterios?.message) && (
        <p className="text-meta font-medium text-error">{errors.criterios?.root?.message ?? errors.criterios?.message}</p>
      )}
      <div className="flex flex-col gap-4">
        {evaluacion.criterios.map((criterio, indice) => (
          <CamposAjusteCriterio
            key={criterio.criterioId}
            indice={indice}
            criterio={criterio}
            control={control}
            register={register}
            errors={errors}
            segmentos={segmentos}
          />
        ))}
      </div>
      <div className="flex justify-end gap-2 border-t border-borde pt-5">
        <Boton variante="secundario" onClick={onCancelar}>
          Cancelar
        </Boton>
        <Boton type="submit" cargando={isSubmitting} disabled={!isValid}>
          Guardar ajuste
        </Boton>
      </div>
    </form>
  )
}
