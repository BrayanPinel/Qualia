import { useState } from 'react'
import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { aplicarErroresAlFormulario } from '../../../shared/api/extraerMensajeErrorBackend'
import { Boton } from '../../../shared/components/Boton'
import { AlertaFormulario, CampoSelector, CampoTexto } from '../../../shared/components/Formulario'
import { ETIQUETA_ROL, ROLES_EMPRESA } from '../../../shared/types/comunes.types'
import { crearUsuario } from '../api/usuariosApi'
import { crearUsuarioSchema, type CrearUsuarioEntrada, type CrearUsuarioFormulario } from '../schemas/usuario.schema'

const CAMPOS = ['nombre', 'email', 'password', 'rol'] as const

interface PropsCrearUsuario {
  onCreado: () => void
  onCancelar: () => void
}

export function CrearUsuarioForm({ onCreado, onCancelar }: PropsCrearUsuario) {
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isValid, isSubmitting },
  } = useForm<CrearUsuarioEntrada, unknown, CrearUsuarioFormulario>({
    resolver: zodResolver(crearUsuarioSchema),
    mode: 'onChange',
    defaultValues: { nombre: '', email: '', password: '', rol: '' },
  })

  const enviar = async (datos: CrearUsuarioFormulario) => {
    setErrorGeneral(null)
    try {
      await crearUsuario(datos)
      onCreado()
    } catch (error) {
      setErrorGeneral(aplicarErroresAlFormulario(error, setError, CAMPOS))
    }
  }

  return (
    <form onSubmit={handleSubmit(enviar)} noValidate className="flex flex-col gap-5">
      <AlertaFormulario mensaje={errorGeneral} />
      <CampoTexto etiqueta="Nombre" autoComplete="off" error={errors.nombre?.message} {...register('nombre')} />
      <CampoTexto etiqueta="Email" type="email" autoComplete="off" error={errors.email?.message} {...register('email')} />
      <CampoTexto
        etiqueta="Contraseña inicial"
        type="password"
        autoComplete="new-password"
        ayuda="Entre 12 y 128 caracteres. Compártela con la persona por un canal seguro."
        error={errors.password?.message}
        {...register('password')}
      />
      <CampoSelector etiqueta="Rol" error={errors.rol?.message} {...register('rol')}>
        <option value="">Selecciona un rol</option>
        {ROLES_EMPRESA.map((rol) => (
          <option key={rol} value={rol}>
            {ETIQUETA_ROL[rol]}
          </option>
        ))}
      </CampoSelector>
      <div className="flex justify-end gap-2 border-t border-borde pt-5">
        <Boton variante="secundario" onClick={onCancelar}>
          Cancelar
        </Boton>
        <Boton type="submit" cargando={isSubmitting} disabled={!isValid}>
          Crear usuario
        </Boton>
      </div>
    </form>
  )
}
