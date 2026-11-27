import { useEffect, useState } from 'react'
import { zodResolver } from '@hookform/resolvers/zod'
import { useForm, useWatch } from 'react-hook-form'
import { aplicarErroresAlFormulario } from '../../../shared/api/extraerMensajeErrorBackend'
import { Boton } from '../../../shared/components/Boton'
import { EstadoError } from '../../../shared/components/EstadosVista'
import { AlertaFormulario, CampoSelector } from '../../../shared/components/Formulario'
import { IconoMas } from '../../../shared/components/Iconos'
import { useConsulta } from '../../../shared/hooks/useConsulta'
import { ETIQUETA_ROL, type UsuarioDto } from '../../../shared/types/comunes.types'
import { asignarUsuario, listarCandidatos } from '../api/campanasApi'
import { asignarUsuarioSchema, type AsignarUsuarioFormulario } from '../schemas/campana.schema'

const CAMPOS = ['usuarioId'] as const

interface PropsAsignarUsuario {
  campanaId: string
  asignados: UsuarioDto[]
  onAsignado: () => void
}

/** Selector de agentes y supervisores activos que aún no están en la campaña. */
export function AsignarUsuarioForm({ campanaId, asignados, onAsignado }: PropsAsignarUsuario) {
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null)
  const candidatos = useConsulta('candidatos-campana', listarCandidatos)
  const {
    control,
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isValid, isSubmitting, isSubmitSuccessful },
  } = useForm<AsignarUsuarioFormulario>({
    resolver: zodResolver(asignarUsuarioSchema),
    mode: 'onChange',
    defaultValues: { usuarioId: '' },
  })

  // Se limpia el selector cuando handleSubmit termina. keepFieldsRef conserva la referencia al <select>:
  // sin ella reset() la descarta y, como el React Compiler memoriza las props de register, no se vuelve a registrar.
  useEffect(() => {
    if (isSubmitSuccessful) reset({ usuarioId: '' }, { keepFieldsRef: true })
  }, [isSubmitSuccessful, reset])

  // reset no recalcula isValid: el botón también se bloquea mientras no haya nadie elegido.
  const usuarioElegido = useWatch({ control, name: 'usuarioId' })

  const yaAsignados = new Set(asignados.map((u) => u.id))
  const opciones = (candidatos.datos ?? []).filter((u) => !yaAsignados.has(u.id))

  const enviar = async ({ usuarioId }: AsignarUsuarioFormulario) => {
    setErrorGeneral(null)
    try {
      await asignarUsuario(campanaId, usuarioId)
      onAsignado()
    } catch (error) {
      // 403 LIMITE_PLAN_SUPERVISORES, 409 ROL_NO_ASIGNABLE o USUARIO_INACTIVO: mensaje del backend.
      setErrorGeneral(aplicarErroresAlFormulario(error, setError, CAMPOS))
    }
  }

  if (candidatos.error) return <EstadoError mensaje={candidatos.error} onReintentar={candidatos.recargar} />

  return (
    <form onSubmit={handleSubmit(enviar)} noValidate className="flex flex-col gap-3">
      <AlertaFormulario mensaje={errorGeneral} />
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start">
        <div className="flex-1">
          <CampoSelector
            etiqueta="Asignar persona"
            disabled={candidatos.cargando && !candidatos.datos}
            ayuda={candidatos.datos && opciones.length === 0 ? 'Todos los agentes y supervisores activos ya están asignados.' : undefined}
            error={errors.usuarioId?.message}
            {...register('usuarioId')}
          >
            <option value="">{candidatos.datos ? 'Selecciona un agente o supervisor' : 'Cargando personas…'}</option>
            {opciones.map((u) => (
              <option key={u.id} value={u.id}>
                {u.nombre} · {ETIQUETA_ROL[u.rol]}
              </option>
            ))}
          </CampoSelector>
        </div>
        <Boton type="submit" icono={<IconoMas tamano={18} />} cargando={isSubmitting} disabled={!isValid || !usuarioElegido} className="sm:mt-7">
          Asignar
        </Boton>
      </div>
    </form>
  )
}
