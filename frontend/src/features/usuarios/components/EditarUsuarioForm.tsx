import { useState } from 'react'
import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { aplicarErroresAlFormulario } from '../../../shared/api/extraerMensajeErrorBackend'
import { Boton } from '../../../shared/components/Boton'
import { AlertaFormulario, CampoCasilla, CampoTexto } from '../../../shared/components/Formulario'
import type { UsuarioDto } from '../../../shared/types/comunes.types'
import { ETIQUETA_ROL } from '../../../shared/types/comunes.types'
import { actualizarUsuario } from '../api/usuariosApi'
import { actualizarUsuarioSchema, type ActualizarUsuarioFormulario } from '../schemas/usuario.schema'

const CAMPOS = ['nombre', 'activo'] as const

interface PropsEditarUsuario {
  usuario: UsuarioDto
  onGuardado: () => void
  onCancelar: () => void
}

/** Edita nombre y estado. Email y rol no se cambian por contrato. */
export function EditarUsuarioForm({ usuario, onGuardado, onCancelar }: PropsEditarUsuario) {
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isValid, isDirty, isSubmitting },
  } = useForm<ActualizarUsuarioFormulario>({
    resolver: zodResolver(actualizarUsuarioSchema),
    mode: 'onChange',
    defaultValues: { nombre: usuario.nombre, activo: usuario.activo },
  })

  const enviar = async (datos: ActualizarUsuarioFormulario) => {
    setErrorGeneral(null)
    try {
      await actualizarUsuario(usuario.id, datos)
      onGuardado()
    } catch (error) {
      setErrorGeneral(aplicarErroresAlFormulario(error, setError, CAMPOS))
    }
  }

  return (
    <form onSubmit={handleSubmit(enviar)} noValidate className="flex flex-col gap-5">
      <AlertaFormulario mensaje={errorGeneral} />
      <dl className="grid grid-cols-[auto_1fr] gap-x-4 gap-y-1 rounded-lg bg-alabastro px-4 py-3 text-sm">
        <dt className="text-texto-secundario">Email</dt>
        <dd className="truncate">{usuario.email}</dd>
        <dt className="text-texto-secundario">Rol</dt>
        <dd>{ETIQUETA_ROL[usuario.rol]}</dd>
      </dl>
      <CampoTexto etiqueta="Nombre" autoComplete="off" error={errors.nombre?.message} {...register('nombre')} />
      <CampoCasilla
        etiqueta="Usuario activo"
        ayuda="Un usuario inactivo no puede iniciar sesión y su sesión actual deja de valer."
        error={errors.activo?.message}
        {...register('activo')}
      />
      <div className="flex justify-end gap-2 border-t border-borde pt-5">
        <Boton variante="secundario" onClick={onCancelar}>
          Cancelar
        </Boton>
        <Boton type="submit" cargando={isSubmitting} disabled={!isValid || !isDirty}>
          Guardar cambios
        </Boton>
      </div>
    </form>
  )
}
