import { useState } from 'react'
import { Link, Outlet } from 'react-router-dom'
import { useSesion } from '../features/auth/context/useSesion'
import { IconoCerrar, IconoMenu } from '../shared/components/Iconos'
import { Logo } from '../shared/components/Logo'
import { MenuNavegacion } from './MenuNavegacion'
import { rutaInicioPorRol } from './navegacion'

/**
 * Sidebar carbón de 240 px en escritorio; en móvil, cabecera con el logotipo y un menú desplegable.
 * El contenido de cada ruta se pinta en el Outlet.
 */
export function AppLayout() {
  const { sesion } = useSesion()
  const [menuAbierto, setMenuAbierto] = useState(false)
  const inicio = sesion ? rutaInicioPorRol(sesion.rol) : '/'

  return (
    <div className="min-h-screen bg-alabastro">
      <a
        href="#contenido"
        className="sr-only z-50 rounded-lg bg-superficie px-4 py-2 focus:not-sr-only focus:fixed focus:top-4 focus:left-4"
      >
        Saltar al contenido
      </a>

      <aside className="fixed inset-y-0 left-0 hidden w-60 flex-col bg-carbon px-4 py-6 lg:flex">
        <Link to={inicio} className="mb-8 inline-flex px-3" aria-label="Qualia, ir al inicio">
          <Logo tono="claro" tamano="sm" />
        </Link>
        <MenuNavegacion />
      </aside>

      <header className="sticky top-0 z-30 bg-carbon lg:hidden">
        <div className="flex h-16 items-center justify-between px-4">
          <Link to={inicio} aria-label="Qualia, ir al inicio">
            <Logo tono="claro" tamano="sm" />
          </Link>
          <button
            type="button"
            onClick={() => setMenuAbierto((abierto) => !abierto)}
            aria-expanded={menuAbierto}
            aria-controls="menu-movil"
            aria-label={menuAbierto ? 'Cerrar menú' : 'Abrir menú'}
            className="grid size-11 place-items-center rounded-lg text-alabastro hover:bg-sidebar-activo"
          >
            {menuAbierto ? <IconoCerrar tamano={24} /> : <IconoMenu tamano={24} />}
          </button>
        </div>
        {menuAbierto && (
          <div id="menu-movil" className="max-h-[calc(100vh-4rem)] overflow-y-auto border-t border-sidebar-activo px-4 py-4">
            <MenuNavegacion onNavegar={() => setMenuAbierto(false)} />
          </div>
        )}
      </header>

      <main id="contenido" className="px-4 py-6 sm:px-6 lg:ml-60 lg:px-8 lg:py-8">
        <div className="mx-auto max-w-7xl">
          <Outlet />
        </div>
      </main>
    </div>
  )
}
