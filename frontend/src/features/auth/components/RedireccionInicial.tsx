import { Navigate } from 'react-router-dom'
import { rutaInicioPorRol } from '../../../layouts/navegacion'
import { useSesion } from '../context/useSesion'

/** "/" lleva al inicio del rol si hay sesión, o al login si no. */
export function RedireccionInicial() {
  const { sesion } = useSesion()
  return <Navigate to={sesion ? rutaInicioPorRol(sesion.rol) : '/login'} replace />
}
