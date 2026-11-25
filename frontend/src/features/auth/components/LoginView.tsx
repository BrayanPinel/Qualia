import { useState } from 'react'
import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { Link, Navigate, useLocation, useSearchParams } from 'react-router-dom'
import { AuthLayout } from '../../../layouts/AuthLayout'
import { rutaInicioPorRol } from '../../../layouts/navegacion'
import { aplicarErroresAlFormulario } from '../../../shared/api/extraerMensajeErrorBackend'
import { Boton } from '../../../shared/components/Boton'
import { AlertaFormulario, CampoTexto } from '../../../shared/components/Formulario'
import { iniciarSesion } from '../api/authApi'
import { useSesion } from '../context/useSesion'
import { loginSchema, type LoginFormulario } from '../schemas/login.schema'

const CAMPOS = ['codigoEmpresa', 'email', 'password'] as const

/** Login de usuarios de empresa (AdminEmpresa, Supervisor, Agente, Auditor). */
export function LoginView() {
  const { sesion, abrirSesion } = useSesion()
  const ubicacion = useLocation()
  const [parametros] = useSearchParams()
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isValid, isSubmitting },
  } = useForm<LoginFormulario>({
    resolver: zodResolver(loginSchema),
    mode: 'onChange',
    defaultValues: { codigoEmpresa: parametros.get('codigo') ?? '', email: '', password: '' },
  })

  // Al abrir la sesión este mismo componente redirige: a la ruta que se intentó abrir o al inicio del rol.
  const desde = (ubicacion.state as { desde?: string } | null)?.desde
  if (sesion) return <Navigate to={desde ?? rutaInicioPorRol(sesion.rol)} replace />

  const registrada = parametros.has('registrada')

  const enviar = async (datos: LoginFormulario) => {
    setErrorGeneral(null)
    try {
      abrirSesion(await iniciarSesion(datos))
    } catch (error) {
      setErrorGeneral(aplicarErroresAlFormulario(error, setError, CAMPOS))
    }
  }

  return (
    <AuthLayout titulo="Iniciar sesión" descripcion="Accede con el código de tu empresa y tu cuenta.">
      <form onSubmit={handleSubmit(enviar)} noValidate className="flex flex-col gap-5">
        {registrada && (
          <p role="status" className="rounded-lg border border-exito/30 bg-exito-fondo px-4 py-3 text-sm text-exito">
            Empresa registrada. Inicia sesión con la cuenta de administrador que creaste.
          </p>
        )}
        <AlertaFormulario mensaje={errorGeneral} />
        <CampoTexto
          etiqueta="Código de empresa"
          autoComplete="organization"
          ayuda="El identificador corto de tu empresa, por ejemplo demo-norte."
          error={errors.codigoEmpresa?.message}
          {...register('codigoEmpresa')}
        />
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

      <div className="mt-8 flex flex-col gap-3 border-t border-borde pt-6 text-sm text-texto-secundario">
        <p>
          ¿Tu empresa aún no usa Qualia?{' '}
          <Link to="/registro" className="font-medium text-oliva underline-offset-4 hover:underline">
            Registrar empresa
          </Link>
        </p>
        <p>
          <Link to="/plataforma/login" className="font-medium text-carbon underline-offset-4 hover:underline">
            Acceso de administración de plataforma
          </Link>
        </p>
      </div>
    </AuthLayout>
  )
}
