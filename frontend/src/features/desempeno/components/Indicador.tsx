import type { ReactNode } from 'react'

interface PropsIndicador {
  etiqueta: string
  valor: ReactNode
  contexto?: ReactNode
}

/** Tarjeta de indicador: etiqueta, dato grande y contexto en texto secundario. */
export function Indicador({ etiqueta, valor, contexto }: PropsIndicador) {
  return (
    <div className="flex flex-col gap-1 rounded-lg border border-borde bg-superficie p-5">
      <p className="text-meta text-texto-secundario">{etiqueta}</p>
      <p className="font-titulo text-h2 font-bold tabular-nums">{valor}</p>
      {contexto && <p className="text-meta text-texto-secundario">{contexto}</p>}
    </div>
  )
}
