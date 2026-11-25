import { Boton } from './Boton'
import { IconoFlechaDer, IconoFlechaIzq } from './Iconos'

interface PropsPaginador {
  pagina: number
  tamanoPagina: number
  total: number
  onCambiar: (pagina: number) => void
  deshabilitado?: boolean
}

/** Paginación del backend (pagina desde 1, tamanoPagina, total). */
export function Paginador({ pagina, tamanoPagina, total, onCambiar, deshabilitado = false }: PropsPaginador) {
  const paginas = Math.max(1, Math.ceil(total / tamanoPagina))
  const desde = total === 0 ? 0 : (pagina - 1) * tamanoPagina + 1
  const hasta = Math.min(pagina * tamanoPagina, total)

  return (
    <nav
      aria-label="Paginación"
      className="flex flex-wrap items-center justify-between gap-3 border-t border-borde px-6 py-3 text-sm"
    >
      <p className="text-texto-secundario tabular-nums">
        {desde}–{hasta} de {total}
      </p>
      <div className="flex items-center gap-2">
        <Boton
          variante="secundario"
          tamano="sm"
          icono={<IconoFlechaIzq tamano={18} />}
          disabled={deshabilitado || pagina <= 1}
          onClick={() => onCambiar(pagina - 1)}
        >
          Anterior
        </Boton>
        <span className="px-2 tabular-nums" aria-current="page">
          {pagina} / {paginas}
        </span>
        <Boton
          variante="secundario"
          tamano="sm"
          disabled={deshabilitado || pagina >= paginas}
          onClick={() => onCambiar(pagina + 1)}
        >
          Siguiente
          <IconoFlechaDer tamano={18} />
        </Boton>
      </div>
    </nav>
  )
}
