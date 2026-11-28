import { useState } from 'react'
import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { aplicarErroresAlFormulario } from '../../../shared/api/extraerMensajeErrorBackend'
import { Boton } from '../../../shared/components/Boton'
import { AlertaFormulario, CampoAreaTexto } from '../../../shared/components/Formulario'
import { crearDisputa } from '../api/disputasApi'
import { crearDisputaSchema, type CrearDisputaFormulario } from '../schemas/disputa.schema'
import type { DisputaDto } from '../types/disputa.types'

const CAMPOS = ['motivo'] as const

interface PropsDisputarForm {
  evaluacionId: string
  onCreada: (disputa: DisputaDto) => void
  onCancelar: () => void
}

/** Motivo de la disputa. DISPUTA_ABIERTA_EXISTENTE, EVALUACION_NO_OFICIAL o FUNCION_NO_INCLUIDA se muestran tal cual. */
export function DisputarForm({ evaluacionId, onCreada, onCancelar }: PropsDisputarForm) {
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isValid, isSubmitting },
  } = useForm<CrearDisputaFormulario>({ resolver: zodResolver(crearDisputaSchema), mode: 'onChange', defaultValues: { motivo: '' } })

  const enviar = async (datos: CrearDisputaFormulario) => {
    setErrorGeneral(null)
    try {
      onCreada(await crearDisputa(evaluacionId, datos))
    } catch (error) {
      setErrorGeneral(aplicarErroresAlFormulario(error, setError, CAMPOS))
    }
  }

  return (
    <form onSubmit={handleSubmit(enviar)} noValidate className="flex flex-col gap-5">
      <AlertaFormulario mensaje={errorGeneral} />
      <CampoAreaTexto
        etiqueta="Motivo"
        rows={5}
        ayuda="Explica qué criterio consideras mal evaluado y por qué. Entre 10 y 2000 caracteres."
        error={errors.motivo?.message}
        {...register('motivo')}
      />
      <div className="flex justify-end gap-2 border-t border-borde pt-5">
        <Boton variante="secundario" onClick={onCancelar}>
          Cancelar
        </Boton>
        <Boton type="submit" cargando={isSubmitting} disabled={!isValid}>
          Enviar disputa
        </Boton>
      </div>
    </form>
  )
}
