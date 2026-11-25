import type { ReactNode } from 'react'
import { IconoAlerta, IconoCheck, IconoInfo, IconoReloj, IconoX } from './Iconos'

// Etiqueta compacta de estado: color + icono + texto (nunca solo color).
// exito/alerta/error son resultados; neutro y proceso describen estados que no juzgan la calidad.
export type TonoBadge = 'exito' | 'alerta' | 'error' | 'neutro' | 'proceso'

const ESTILOS: Record<TonoBadge, string> = {
  exito: 'bg-exito-fondo text-exito',
  alerta: 'bg-alerta-fondo text-alerta',
  error: 'bg-error-fondo text-error',
  neutro: 'bg-piedra text-carbon',
  proceso: 'bg-superficie text-texto-secundario border border-borde',
}

const ICONOS: Record<TonoBadge, ReactNode> = {
  exito: <IconoCheck tamano={14} />,
  alerta: <IconoAlerta tamano={14} />,
  error: <IconoX tamano={14} />,
  neutro: <IconoInfo tamano={14} />,
  proceso: <IconoReloj tamano={14} />,
}

interface PropsBadge {
  tono: TonoBadge
  children: ReactNode
  /** Sustituye el icono por defecto del tono (por ejemplo, un candado para "Oficial"). */
  icono?: ReactNode
  sinIcono?: boolean
}

export function Badge({ tono, children, icono, sinIcono = false }: PropsBadge) {
  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-md px-2 py-0.5 text-meta font-medium whitespace-nowrap ${ESTILOS[tono]}`}
    >
      {!sinIcono && (icono ?? ICONOS[tono])}
      {children}
    </span>
  )
}
