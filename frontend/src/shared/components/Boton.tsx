import type { ButtonHTMLAttributes, ReactNode } from 'react'
import { IconoGiro } from './Iconos'

// primario: oliva, una sola acción principal por sección. secundario: blanco con borde.
// fantasma: acciones discretas (tablas, cabeceras). peligro: acciones destructivas.
type Variante = 'primario' | 'secundario' | 'fantasma' | 'peligro'

interface PropsBoton extends ButtonHTMLAttributes<HTMLButtonElement> {
  variante?: Variante
  tamano?: 'md' | 'sm'
  cargando?: boolean
  icono?: ReactNode
  anchoCompleto?: boolean
}

const VARIANTES: Record<Variante, string> = {
  primario: 'bg-oliva text-superficie hover:bg-oliva-hover border border-oliva hover:border-oliva-hover',
  secundario: 'bg-superficie text-carbon border border-borde hover:bg-alabastro',
  fantasma: 'bg-transparent text-carbon border border-transparent hover:bg-piedra',
  peligro: 'bg-superficie text-error border border-borde hover:bg-error-fondo',
}

const TAMANOS = { md: 'h-11 px-4 text-sm', sm: 'h-9 px-3 text-sm' } as const

export function Boton({
  variante = 'primario',
  tamano = 'md',
  cargando = false,
  icono,
  anchoCompleto = false,
  disabled,
  className = '',
  children,
  type = 'button',
  ...props
}: PropsBoton) {
  return (
    <button
      type={type}
      disabled={disabled || cargando}
      aria-busy={cargando || undefined}
      className={`inline-flex items-center justify-center gap-2 rounded-lg font-medium whitespace-nowrap transition-colors duration-150 disabled:cursor-not-allowed disabled:opacity-50 ${VARIANTES[variante]} ${TAMANOS[tamano]} ${anchoCompleto ? 'w-full' : ''} ${className}`}
      {...props}
    >
      {cargando ? <IconoGiro tamano={18} className="animate-spin" /> : icono}
      {children}
    </button>
  )
}
