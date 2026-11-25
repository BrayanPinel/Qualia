import { useState } from 'react'
import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { Link, useNavigate } from 'react-router-dom'
import { AuthLayout } from '../../../layouts/AuthLayout'
import { aplicarErroresAlFormulario } from '../../../shared/api/extraerMensajeErrorBackend'
import { Boton } from '../../../shared/components/Boton'
import { AlertaFormulario, CampoTexto } from '../../../shared/components/Formulario'
import { registrarEmpresa } from '../api/empresasApi'
import { registroEmpresaSchema, type RegistroEmpresaFormulario } from '../schemas/registroEmpresa.schema'
import { SelectorPlan } from './SelectorPlan'

const CAMPOS = ['codigoEmpresa', 'nombreEmpresa', 'planId', 'nombreAdministrador', 'email', 'password'] as const

/** Registro público: crea la empresa, su AdminEmpresa y la suscripción de prueba en una sola operación. */
export function RegistroEmpresaView() {
  const navegar = useNavigate()
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isValid, isSubmitting },
  } = useForm<RegistroEmpresaFormulario>({
    resolver: zodResolver(registroEmpresaSchema),
    mode: 'onChange',
    defaultValues: { codigoEmpresa: '', nombreEmpresa: '', planId: '', nombreAdministrador: '', email: '', password: '' },
  })

  const enviar = async (datos: RegistroEmpresaFormulario) => {
    setErrorGeneral(null)
    try {
      const empresa = await registrarEmpresa(datos)
      navegar(`/login?codigo=${encodeURIComponent(empresa.codigoEmpresa)}&registrada=1`, { replace: true })
    } catch (error) {
      setErrorGeneral(aplicarErroresAlFormulario(error, setError, CAMPOS))
    }
  }

  return (
    <AuthLayout
      ancho="amplio"
      titulo="Registrar empresa"
      descripcion="Crea el espacio de tu empresa y la cuenta de su administrador."
    >
      <form onSubmit={handleSubmit(enviar)} noValidate className="flex flex-col gap-8">
        <AlertaFormulario mensaje={errorGeneral} />

        <section className="grid gap-5 sm:grid-cols-2">
          <h2 className="font-titulo text-h3 font-semibold sm:col-span-2">Empresa</h2>
          <CampoTexto
            etiqueta="Código de empresa"
            ayuda="De 3 a 40 caracteres: letras, números y guiones. Se usa para iniciar sesión."
            autoComplete="off"
            error={errors.codigoEmpresa?.message}
            {...register('codigoEmpresa')}
          />
          <CampoTexto
            etiqueta="Nombre de la empresa"
            autoComplete="organization"
            error={errors.nombreEmpresa?.message}
            {...register('nombreEmpresa')}
          />
        </section>

        <SelectorPlan error={errors.planId?.message} {...register('planId')} />

        <section className="grid gap-5 sm:grid-cols-2">
          <h2 className="font-titulo text-h3 font-semibold sm:col-span-2">Administrador</h2>
          <CampoTexto
            etiqueta="Nombre completo"
            autoComplete="name"
            error={errors.nombreAdministrador?.message}
            {...register('nombreAdministrador')}
          />
          <CampoTexto etiqueta="Email" type="email" autoComplete="email" error={errors.email?.message} {...register('email')} />
          <div className="sm:col-span-2">
            <CampoTexto
              etiqueta="Contraseña"
              type="password"
              autoComplete="new-password"
              ayuda="Entre 12 y 128 caracteres."
              error={errors.password?.message}
              {...register('password')}
            />
          </div>
        </section>

        <div className="flex flex-col-reverse items-stretch gap-4 border-t border-borde pt-6 sm:flex-row sm:items-center sm:justify-between">
          <p className="text-sm text-texto-secundario">
            ¿Ya tienes cuenta?{' '}
            <Link to="/login" className="font-medium text-oliva underline-offset-4 hover:underline">
              Iniciar sesión
            </Link>
          </p>
          <Boton type="submit" cargando={isSubmitting} disabled={!isValid}>
            Registrar empresa
          </Boton>
        </div>
      </form>
    </AuthLayout>
  )
}
