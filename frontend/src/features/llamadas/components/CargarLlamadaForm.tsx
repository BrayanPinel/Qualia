import { useState } from 'react'
import { zodResolver } from '@hookform/resolvers/zod'
import { Controller, useForm, useWatch } from 'react-hook-form'
import { aplicarErroresAlFormulario, leerErrorDto } from '../../../shared/api/extraerMensajeErrorBackend'
import { Boton } from '../../../shared/components/Boton'
import { AlertaFormulario, CampoSelector, CampoTexto } from '../../../shared/components/Formulario'
import { useConsulta } from '../../../shared/hooks/useConsulta'
import type { CampanaDto } from '../../../shared/types/campanas.types'
import type { Rol } from '../../../shared/types/comunes.types'
import { cargarLlamada, listarAgentesDeCampana, listarAgentesDeCampanaSupervisor } from '../api/llamadasApi'
import {
  cargarLlamadaSchema,
  EXTENSIONES_AUDIO,
  TAMANO_MAXIMO_MB,
  type CargarLlamadaEntrada,
  type CargarLlamadaFormulario,
} from '../schemas/cargarLlamada.schema'

const CAMPOS = ['campanaId', 'agenteId', 'audio'] as const

interface PropsCargarLlamada {
  rol: Rol
  campanas: CampanaDto[]
  onCargada: () => void
  onCancelar: () => void
  /** 403 CARGA_MANUAL_DESHABILITADA: la vista oculta el botón y muestra el mensaje del backend. */
  onDeshabilitada: (mensaje: string) => void
}

/** Carga manual de una grabación (herramienta de Development). La llamada queda Disponible. */
export function CargarLlamadaForm({ rol, campanas, onCargada, onCancelar, onDeshabilitada }: PropsCargarLlamada) {
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null)
  const {
    register,
    control,
    handleSubmit,
    setError,
    setValue,
    formState: { errors, isValid, isSubmitting },
  } = useForm<CargarLlamadaEntrada, unknown, CargarLlamadaFormulario>({
    resolver: zodResolver(cargarLlamadaSchema),
    mode: 'onChange',
    defaultValues: { campanaId: '', agenteId: '', audio: undefined },
  })

  const campanaId = useWatch({ control, name: 'campanaId' })
  const agentes = useConsulta(`cargar-agentes-${campanaId}`, () => {
    if (!campanaId) return Promise.resolve([])
    return rol === 'AdminEmpresa' ? listarAgentesDeCampana(campanaId) : listarAgentesDeCampanaSupervisor(campanaId)
  })

  const enviar = async (datos: CargarLlamadaFormulario) => {
    setErrorGeneral(null)
    try {
      await cargarLlamada(datos)
      onCargada()
    } catch (error) {
      const dto = leerErrorDto(error)
      if (dto?.codigo === 'CARGA_MANUAL_DESHABILITADA') {
        onDeshabilitada(dto.mensaje)
        return
      }
      setErrorGeneral(aplicarErroresAlFormulario(error, setError, CAMPOS))
    }
  }

  return (
    <form onSubmit={handleSubmit(enviar)} noValidate className="flex flex-col gap-5">
      <AlertaFormulario mensaje={errorGeneral ?? agentes.error ?? null} />
      <CampoSelector
        etiqueta="Campaña"
        error={errors.campanaId?.message}
        {...register('campanaId', { onChange: () => setValue('agenteId', '', { shouldValidate: true }) })}
      >
        <option value="">Selecciona una campaña</option>
        {campanas.map((c) => (
          <option key={c.id} value={c.id}>
            {c.nombre}
          </option>
        ))}
      </CampoSelector>
      <CampoSelector
        etiqueta="Agente"
        disabled={!campanaId || agentes.cargando}
        error={errors.agenteId?.message}
        {...register('agenteId')}
      >
        <option value="">{agentes.cargando && campanaId ? 'Cargando agentes…' : 'Selecciona un agente'}</option>
        {(agentes.datos ?? []).map((a) => (
          <option key={a.id} value={a.id}>
            {a.nombre}
          </option>
        ))}
      </CampoSelector>
      <Controller
        control={control}
        name="audio"
        render={({ field: { name, ref, onBlur, onChange } }) => (
          // El input de archivo no se controla con value: solo se envía el File elegido.
          <CampoTexto
            etiqueta="Grabación"
            type="file"
            accept={EXTENSIONES_AUDIO.join(',')}
            ayuda={`${EXTENSIONES_AUDIO.join(', ')}. Máximo ${TAMANO_MAXIMO_MB} MB.`}
            error={errors.audio?.message}
            className="py-2 file:mr-3 file:rounded-md file:border-0 file:bg-piedra file:px-3 file:py-1 file:text-sm"
            {...{ name, ref, onBlur }}
            onChange={(e) => onChange(e.target.files?.[0])}
          />
        )}
      />
      <div className="flex justify-end gap-2 border-t border-borde pt-5">
        <Boton variante="secundario" onClick={onCancelar}>
          Cancelar
        </Boton>
        <Boton type="submit" cargando={isSubmitting} disabled={!isValid}>
          Cargar llamada
        </Boton>
      </div>
    </form>
  )
}
