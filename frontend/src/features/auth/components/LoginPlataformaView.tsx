import { useState } from 'react'
import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { Link, Navigate } from 'react-router-dom'
import { AuthLayout } from '../../../layouts/AuthLayout'
import { rutaInicioPorRol } from '../../../layouts/navegacion'
import { aplicarErroresAlFormulario } from '../../../shared/api/extraerMensajeErrorBackend'
import { Boton } from '../../../shared/components/Boton'
import { AlertaFormulario, CampoTexto } from '../../../shared/components/Formulario'
import { iniciarSesionPlataforma } from '../api/authApi'
import { useSesion } from '../context/useSesion'
import { loginPlataformaSchema, type LoginPlataformaFormulario } from '../schemas/login.schema'

const CAMPOS = ['email', 'password'] as const

/** Login del Administrador global (PlatformAdmin). Acceso separado y sin empresa. */
export function LoginPlataformaView() {
  const { sesion, abrirSesion } = useSesion()
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isValid, isSubmitting },
  } = useForm<LoginPlataformaFormulario>({
    resolver: zodResolver(loginPlataformaSchema),
    mode: 'onChange',
    defaultValues: { email: '', password: '' },
  })

  if (sesion) return <Navigate to={rutaInicioPorRol(sesion.rol)} replace />

  const enviar = async (datos: LoginPlataformaFormulario) => {
    setErrorGeneral(null)
    try {
      // Con la sesión abierta, este componente redirige al inicio del rol.
      abrirSesion(await iniciarSesionPlataforma(datos))
    } catch (error) {
      setErrorGeneral(aplicarErroresAlFormulario(error, setError, CAMPOS))
    }
  }

  return (
    <AuthLayout
      titulo="Administración de plataforma"
      descripcion="Acceso para la gestión comercial de empresas y suscripciones."
    >
      <form onSubmit={handleSubmit(enviar)} noValidate className="flex flex-col gap-5">
        <AlertaFormulario mensaje={errorGeneral} />
        <CampoTexto etiqueta="Email" type="email" autoComplete="username" error={errors.email?.message} {...register('email')} />
        <CampoTexto
          etiqueta="Contraseña"
          type="password"
          autoComplete="current-password"
          error={errors.password?.message}
          {...register('password')}
        />
        <Boton type="submit" anchoCompleto cargando={isSubmitting} disabled={!isValid}>
          Iniciar sesión
        </Boton>
      </form>

      <p className="mt-8 border-t border-borde pt-6 text-sm text-texto-secundario">
        ¿Trabajas en una empresa cliente?{' '}
        <Link to="/login" className="font-medium text-oliva underline-offset-4 hover:underline">
          Iniciar sesión con tu empresa
        </Link>
      </p>
    </AuthLayout>
  )
}
