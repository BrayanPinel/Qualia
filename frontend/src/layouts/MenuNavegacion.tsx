import { NavLink } from 'react-router-dom'
import { useSesion } from '../features/auth/context/useSesion'
import { IconoSalir } from '../shared/components/Iconos'
import { ETIQUETA_ROL } from '../shared/types/comunes.types'
import { etiquetaPara, menuPorRol } from './navegacion'

interface PropsMenu {
  /** Se llama al elegir una opción (cierra el menú desplegable en móvil). */
  onNavegar?: () => void
}

/** Opciones del rol, usuario, empresa y "Cerrar sesión", sobre fondo carbón. */
export function MenuNavegacion({ onNavegar }: PropsMenu) {
  const { sesion, cerrarSesion } = useSesion()
  if (!sesion) return null

  return (
    <div className="flex h-full flex-col">
      <nav aria-label="Principal" className="flex-1">
        <ul className="flex flex-col gap-1">
          {menuPorRol(sesion.rol).map((item) => (
            <li key={item.ruta}>
              <NavLink
                to={item.ruta}
                onClick={onNavegar}
                className={({ isActive }) =>
                  `flex min-h-11 items-center gap-3 rounded-lg px-3 text-sm font-medium transition-colors ${
                    isActive
                      ? 'bg-sidebar-activo text-alabastro'
                      : 'text-sidebar-texto hover:bg-sidebar-activo/60 hover:text-alabastro'
                  }`
                }
              >
                <item.icono tamano={20} />
                {etiquetaPara(item, sesion.rol)}
              </NavLink>
            </li>
          ))}
        </ul>
      </nav>

      <div className="mt-6 border-t border-sidebar-activo pt-4">
        <div className="flex items-center gap-3 px-3">
          <span
            aria-hidden="true"
            className="grid size-9 shrink-0 place-items-center rounded-full bg-sidebar-activo text-sm font-semibold text-alabastro"
          >
            {sesion.usuario.nombre.charAt(0).toUpperCase()}
          </span>
          <div className="min-w-0">
            <p className="truncate text-sm font-medium text-alabastro">{sesion.usuario.nombre}</p>
            <p className="truncate text-meta text-sidebar-texto">
              {ETIQUETA_ROL[sesion.rol]}
              {sesion.codigoEmpresa && ` · ${sesion.codigoEmpresa}`}
            </p>
          </div>
        </div>
        <button
          type="button"
          onClick={cerrarSesion}
          className="mt-3 flex min-h-11 w-full items-center gap-3 rounded-lg px-3 text-sm font-medium text-sidebar-texto hover:bg-sidebar-activo/60 hover:text-alabastro"
        >
          <IconoSalir tamano={20} />
          Cerrar sesión
        </button>
      </div>
    </div>
  )
}
