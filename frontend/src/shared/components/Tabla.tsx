import type { ReactNode } from 'react'

export interface ColumnaTabla<T> {
  clave: string
  encabezado: string
  /** Números a la derecha; texto a la izquierda. */
  alinear?: 'izquierda' | 'derecha'
  celda: (fila: T) => ReactNode
  /** Oculta la columna en pantallas pequeñas (información secundaria). */
  ocultarEnMovil?: boolean
}

interface PropsTabla<T> {
  columnas: ColumnaTabla<T>[]
  filas: T[]
  claveFila: (fila: T) => string
  /** Texto para lectores de pantalla. */
  descripcion: string
}

/** Tabla con encabezados discretos, filas de ~52 px y desplazamiento horizontal localizado en móvil. */
export function Tabla<T>({ columnas, filas, claveFila, descripcion }: PropsTabla<T>) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full min-w-[40rem] border-collapse text-left text-sm">
        <caption className="sr-only">{descripcion}</caption>
        <thead>
          <tr className="border-b border-borde">
            {columnas.map((c) => (
              <th
                key={c.clave}
                scope="col"
                className={`px-6 py-3 text-meta font-medium text-texto-secundario ${c.alinear === 'derecha' ? 'text-right' : ''} ${c.ocultarEnMovil ? 'hidden md:table-cell' : ''}`}
              >
                {c.encabezado}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {filas.map((fila) => (
            <tr key={claveFila(fila)} className="h-13 border-b border-borde last:border-b-0 hover:bg-alabastro">
              {columnas.map((c) => (
                <td
                  key={c.clave}
                  className={`px-6 py-3 align-middle ${c.alinear === 'derecha' ? 'text-right tabular-nums' : ''} ${c.ocultarEnMovil ? 'hidden md:table-cell' : ''}`}
                >
                  {c.celda(fila)}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
