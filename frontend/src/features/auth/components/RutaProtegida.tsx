import { Navigate, Outlet, useLocation } from 'react-router-dom'
import type { Rol } from '../../../shared/types/comunes.types'
import { useSesion } from '../context/useSesion'
import { SinPermisoView } from './SinPermisoView'

interface PropsRutaProtegida {
  /** Roles permitidos. Sin roles: basta con tener sesión. */
  roles?: readonly Rol[]
}

/**
 * Guardia de navegación: sin JWT redirige a /login (recordando a dónde iba);
 * con un rol no permitido muestra "Sin permiso". El backend vuelve a validar cada petición.
 */
export function RutaProtegida({ roles }: PropsRutaProtegida) {
  const { sesion } = useSesion()
  const ubicacion = useLocation()

  if (!sesion) {
    const destino = ubicacion.pathname.startsWith('/plataforma') ? '/plataforma/login' : '/login'
    return <Navigate to={destino} replace state={{ desde: ubicacion.pathname }} />
  }

  if (roles && !roles.includes(sesion.rol)) {
    return <SinPermisoView />
  }

  return <Outlet />
}
