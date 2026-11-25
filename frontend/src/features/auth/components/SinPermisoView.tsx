import { Link } from 'react-router-dom'
import { IconoCandado } from '../../../shared/components/Iconos'
import { rutaInicioPorRol } from '../../../layouts/navegacion'
import { useSesion } from '../context/useSesion'

export function SinPermisoView() {
  const { sesion } = useSesion()
  return (
    <div className="mx-auto flex max-w-md flex-col items-center gap-4 py-16 text-center">
      <span className="grid size-14 place-items-center rounded-full bg-piedra text-carbon">
        <IconoCandado tamano={24} />
      </span>
      <h1 className="font-titulo text-h2 font-bold">Sin permiso</h1>
      <p className="text-texto-secundario">Tu rol no tiene acceso a esta sección. Si crees que es un error, consulta al administrador de tu empresa.</p>
      <Link
        to={sesion ? rutaInicioPorRol(sesion.rol) : '/login'}
        className="inline-flex h-11 items-center rounded-lg bg-oliva px-4 text-sm font-medium text-superficie hover:bg-oliva-hover"
      >
        Volver al inicio
      </Link>
    </div>
  )
}
