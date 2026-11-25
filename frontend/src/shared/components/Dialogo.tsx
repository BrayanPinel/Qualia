import { useEffect, useId, useRef } from 'react'
import type { ReactNode } from 'react'
import { IconoCerrar } from './Iconos'

interface PropsDialogo {
  abierto: boolean
  titulo: string
  descripcion?: string
  onCerrar: () => void
  children: ReactNode
}

/**
 * Diálogo modal sobre el elemento nativo dialog: atrapa el foco, cierra con Esc
 * y devuelve el foco al botón que lo abrió.
 */
export function Dialogo({ abierto, titulo, descripcion, onCerrar, children }: PropsDialogo) {
  const ref = useRef<HTMLDialogElement>(null)
  const idTitulo = useId()

  useEffect(() => {
    const dialogo = ref.current
    if (!dialogo) return
    if (abierto && !dialogo.open) dialogo.showModal()
    if (!abierto && dialogo.open) dialogo.close()
  }, [abierto])

  return (
    <dialog
      ref={ref}
      onClose={onCerrar}
      aria-labelledby={idTitulo}
      className="m-auto w-[calc(100%-2rem)] max-w-lg rounded-lg border border-borde bg-superficie p-0 text-carbon shadow-flotante backdrop:bg-carbon/40"
    >
      {abierto && (
        <div className="flex max-h-[85vh] flex-col">
          <header className="flex items-start justify-between gap-4 border-b border-borde px-6 py-4">
            <div>
              <h2 id={idTitulo} className="font-titulo text-h3 font-semibold">
                {titulo}
              </h2>
              {descripcion && <p className="mt-1 text-sm text-texto-secundario">{descripcion}</p>}
            </div>
            <button
              type="button"
              onClick={onCerrar}
              aria-label="Cerrar"
              className="grid size-10 shrink-0 place-items-center rounded-lg text-texto-secundario hover:bg-piedra hover:text-carbon"
            >
              <IconoCerrar />
            </button>
          </header>
          <div className="overflow-y-auto px-6 py-6">{children}</div>
        </div>
      )}
    </dialog>
  )
}
