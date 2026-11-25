import { useState } from 'react'
import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { aplicarErroresAlFormulario } from '../../../shared/api/extraerMensajeErrorBackend'
import { Boton } from '../../../shared/components/Boton'
import { AlertaFormulario, CampoAreaTexto, CampoCasilla, CampoSelector, CampoTexto } from '../../../shared/components/Formulario'
import { ESTADOS_SUSCRIPCION, ETIQUETA_ESTADO_SUSCRIPCION, type PlanDto } from '../../../shared/types/suscripciones.types'
import { aFechaInput } from '../../../shared/utils/formato'
import { cambiarSuscripcion } from '../api/plataformaApi'
import {
  cambiarSuscripcionSchema,
  type CambiarSuscripcionEntrada,
  type CambiarSuscripcionFormulario,
} from '../schemas/cambiarSuscripcion.schema'
import type { EmpresaComercialDto } from '../types/plataforma.types'

const CAMPOS = ['planId', 'estado', 'fechaVencimiento', 'permiteExcedente', 'motivo'] as const

interface PropsCambiarSuscripcion {
  empresa: EmpresaComercialDto
  planes: PlanDto[]
  onGuardado: () => void
  onCancelar: () => void
}

/** Cambio comercial simulado de la suscripción de una empresa. Queda en la auditoría de plataforma. */
export function CambiarSuscripcionForm({ empresa, planes, onGuardado, onCancelar }: PropsCambiarSuscripcion) {
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null)
  const actual = empresa.suscripcion
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isValid, isSubmitting },
  } = useForm<CambiarSuscripcionEntrada, unknown, CambiarSuscripcionFormulario>({
    resolver: zodResolver(cambiarSuscripcionSchema),
    mode: 'onChange',
    defaultValues: {
      planId: actual?.planId ?? '',
      estado: actual?.estado ?? 'Activa',
      fechaVencimiento: actual ? aFechaInput(actual.fechaVencimiento) : '',
      permiteExcedente: actual?.permiteExcedente ?? false,
      motivo: '',
    },
  })

  const enviar = async (datos: CambiarSuscripcionFormulario) => {
    setErrorGeneral(null)
    try {
      await cambiarSuscripcion(empresa.empresaId, datos)
      onGuardado()
    } catch (error) {
      setErrorGeneral(aplicarErroresAlFormulario(error, setError, CAMPOS))
    }
  }

  return (
    <form onSubmit={handleSubmit(enviar)} noValidate className="flex flex-col gap-5">
      <AlertaFormulario mensaje={errorGeneral} />
      <div className="grid gap-5 sm:grid-cols-2">
        <CampoSelector etiqueta="Plan" error={errors.planId?.message} {...register('planId')}>
          <option value="">Selecciona un plan</option>
          {planes.map((plan) => (
            <option key={plan.id} value={plan.id}>
              {plan.nombre}
            </option>
          ))}
        </CampoSelector>
        <CampoSelector etiqueta="Estado" error={errors.estado?.message} {...register('estado')}>
          {ESTADOS_SUSCRIPCION.map((estado) => (
            <option key={estado} value={estado}>
              {ETIQUETA_ESTADO_SUSCRIPCION[estado]}
            </option>
          ))}
        </CampoSelector>
      </div>
      <CampoTexto
        etiqueta="Vence el"
        type="date"
        ayuda="Fin del día en UTC. Si cambia el plan, debe ser una fecha futura."
        error={errors.fechaVencimiento?.message}
        {...register('fechaVencimiento')}
      />
      <CampoCasilla
        etiqueta="Permitir minutos excedentes"
        ayuda="Si está activo, la empresa puede seguir evaluando al agotar los minutos del plan."
        error={errors.permiteExcedente?.message}
        {...register('permiteExcedente')}
      />
      <CampoAreaTexto
        etiqueta="Motivo del cambio"
        ayuda="Entre 10 y 2000 caracteres. Queda registrado en la auditoría de plataforma."
        error={errors.motivo?.message}
        {...register('motivo')}
      />
      <div className="flex justify-end gap-2 border-t border-borde pt-5">
        <Boton variante="secundario" onClick={onCancelar}>
          Cancelar
        </Boton>
        <Boton type="submit" cargando={isSubmitting} disabled={!isValid}>
          Guardar suscripción
        </Boton>
      </div>
    </form>
  )
}
