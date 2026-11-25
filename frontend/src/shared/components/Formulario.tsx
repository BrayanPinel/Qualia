import { useId } from 'react'
import type { InputHTMLAttributes, ReactNode, SelectHTMLAttributes, TextareaHTMLAttributes } from 'react'

// Campos de formulario con etiqueta visible, ayuda y error junto al campo.
// Se usan con React Hook Form: <CampoTexto etiqueta="Email" error={errors.email?.message} {...register('email')} />.
// En React 19 la ref de register llega como prop y se pasa al input con el resto.

const BASE_CONTROL =
  'w-full rounded-lg border bg-superficie px-3 text-base text-carbon placeholder:text-texto-secundario/70 transition-colors disabled:cursor-not-allowed disabled:bg-alabastro disabled:text-texto-secundario'

function bordeSegunError(error?: string) {
  return error ? 'border-error' : 'border-borde hover:border-texto-secundario'
}

interface PropsEnvoltura {
  id: string
  etiqueta: string
  error?: string
  ayuda?: ReactNode
  children: ReactNode
}

function Envoltura({ id, etiqueta, error, ayuda, children }: PropsEnvoltura) {
  return (
    <div className="flex flex-col gap-1.5">
      <label htmlFor={id} className="text-sm font-medium">
        {etiqueta}
      </label>
      {children}
      {ayuda && !error && (
        <p id={`${id}-ayuda`} className="text-meta text-texto-secundario">
          {ayuda}
        </p>
      )}
      {error && (
        <p id={`${id}-error`} className="text-meta font-medium text-error">
          {error}
        </p>
      )}
    </div>
  )
}

function describedBy(id: string, error?: string, ayuda?: ReactNode) {
  if (error) return `${id}-error`
  return ayuda ? `${id}-ayuda` : undefined
}

type PropsComunes = { etiqueta: string; error?: string; ayuda?: ReactNode }

export function CampoTexto({ etiqueta, error, ayuda, className = '', ...props }: PropsComunes & InputHTMLAttributes<HTMLInputElement>) {
  const id = useId()
  return (
    <Envoltura id={id} etiqueta={etiqueta} error={error} ayuda={ayuda}>
      <input
        id={id}
        aria-invalid={Boolean(error)}
        aria-describedby={describedBy(id, error, ayuda)}
        className={`${BASE_CONTROL} h-11 ${bordeSegunError(error)} ${className}`}
        {...props}
      />
    </Envoltura>
  )
}

export function CampoSelector({
  etiqueta,
  error,
  ayuda,
  children,
  ...props
}: PropsComunes & SelectHTMLAttributes<HTMLSelectElement>) {
  const id = useId()
  return (
    <Envoltura id={id} etiqueta={etiqueta} error={error} ayuda={ayuda}>
      <select
        id={id}
        aria-invalid={Boolean(error)}
        aria-describedby={describedBy(id, error, ayuda)}
        className={`${BASE_CONTROL} h-11 ${bordeSegunError(error)}`}
        {...props}
      >
        {children}
      </select>
    </Envoltura>
  )
}

export function CampoAreaTexto({ etiqueta, error, ayuda, ...props }: PropsComunes & TextareaHTMLAttributes<HTMLTextAreaElement>) {
  const id = useId()
  return (
    <Envoltura id={id} etiqueta={etiqueta} error={error} ayuda={ayuda}>
      <textarea
        id={id}
        rows={4}
        aria-invalid={Boolean(error)}
        aria-describedby={describedBy(id, error, ayuda)}
        className={`${BASE_CONTROL} py-2 ${bordeSegunError(error)}`}
        {...props}
      />
    </Envoltura>
  )
}

export function CampoCasilla({ etiqueta, error, ayuda, ...props }: PropsComunes & InputHTMLAttributes<HTMLInputElement>) {
  const id = useId()
  return (
    <div className="flex flex-col gap-1">
      <label htmlFor={id} className="flex min-h-11 cursor-pointer items-center gap-3 text-sm font-medium">
        <input id={id} type="checkbox" className="size-5 rounded accent-oliva" aria-describedby={describedBy(id, error, ayuda)} {...props} />
        {etiqueta}
      </label>
      {ayuda && !error && <p id={`${id}-ayuda`} className="text-meta text-texto-secundario">{ayuda}</p>}
      {error && <p id={`${id}-error`} className="text-meta font-medium text-error">{error}</p>}
    </div>
  )
}

/** Error general del formulario (mensaje del backend que no corresponde a un campo). */
export function AlertaFormulario({ mensaje }: { mensaje: string | null }) {
  if (!mensaje) return null
  return (
    <div role="alert" className="rounded-lg border border-error/30 bg-error-fondo px-4 py-3 text-sm text-error">
      {mensaje}
    </div>
  )
}
