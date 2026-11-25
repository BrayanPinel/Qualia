import type { ReactNode } from 'react'

interface PropsEncabezado {
  titulo: string
  descripcion?: string
  /** Acción principal de la página (un solo botón primario). */
  acciones?: ReactNode
}

export function EncabezadoPagina({ titulo, descripcion, acciones }: PropsEncabezado) {
  return (
    <header className="mb-6 flex flex-wrap items-end justify-between gap-4 md:mb-8">
      <div>
        <h1 className="font-titulo text-h2 font-bold md:text-h1">{titulo}</h1>
        {descripcion && <p className="mt-2 max-w-2xl text-texto-secundario">{descripcion}</p>}
      </div>
      {acciones && <div className="flex flex-wrap gap-2">{acciones}</div>}
    </header>
  )
}
