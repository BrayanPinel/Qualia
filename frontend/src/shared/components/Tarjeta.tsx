import type { ReactNode } from 'react'

interface PropsTarjeta {
  titulo?: string
  descripcion?: string
  acciones?: ReactNode
  children: ReactNode
  /** sinRelleno: para tablas que llegan al borde de la tarjeta. */
  sinRelleno?: boolean
  className?: string
}

/** Superficie blanca con borde sutil. Título, dato y contexto jerarquizados por tamaño y peso. */
export function Tarjeta({ titulo, descripcion, acciones, children, sinRelleno = false, className = '' }: PropsTarjeta) {
  const tieneCabecera = Boolean(titulo || acciones)
  return (
    <section className={`rounded-lg border border-borde bg-superficie ${className}`}>
      {tieneCabecera && (
        <header className="flex flex-wrap items-start justify-between gap-4 px-6 pt-6 pb-4">
          <div>
            {titulo && <h2 className="font-titulo text-h3 font-semibold">{titulo}</h2>}
            {descripcion && <p className="mt-1 text-sm text-texto-secundario">{descripcion}</p>}
          </div>
          {acciones && <div className="flex flex-wrap gap-2">{acciones}</div>}
        </header>
      )}
      <div className={sinRelleno ? '' : tieneCabecera ? 'px-6 pb-6' : 'p-6'}>{children}</div>
    </section>
  )
}
