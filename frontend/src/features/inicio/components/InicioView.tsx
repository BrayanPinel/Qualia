import { Link } from 'react-router-dom'
import { etiquetaPara, menuPorRol } from '../../../layouts/navegacion'
import { EncabezadoPagina } from '../../../shared/components/EncabezadoPagina'
import { IconoFlechaDer } from '../../../shared/components/Iconos'
import { ETIQUETA_ROL } from '../../../shared/types/comunes.types'
import { useSesion } from '../../auth/context/useSesion'

const INTRO_POR_ROL = {
  AdminEmpresa: 'Configura campañas y rúbricas, gestiona a tu equipo y consulta los resultados de la empresa.',
  Supervisor: 'Revisa las evaluaciones que la IA marcó con problemas y resuelve las disputas de tus campañas.',
  Agente: 'Consulta tus llamadas, la evidencia de cada criterio y tu scorecard.',
  Auditor: 'Consulta los reportes agregados de las campañas de la empresa.',
  PlatformAdmin: 'Gestiona las empresas y sus suscripciones.',
} as const

/** Inicio por rol: accesos rápidos a los módulos que el rol puede usar. */
export function InicioView() {
  const { sesion } = useSesion()
  if (!sesion) return null

  const accesos = menuPorRol(sesion.rol).filter((item) => item.ruta !== '/inicio')
  const nombre = sesion.usuario.nombre.split(' ')[0]

  return (
    <>
      <EncabezadoPagina titulo={`Hola, ${nombre}`} descripcion={INTRO_POR_ROL[sesion.rol]} />

      <p className="mb-4 text-sm text-texto-secundario">
        Sesión como <span className="font-medium text-carbon">{ETIQUETA_ROL[sesion.rol]}</span>
        {sesion.codigoEmpresa && (
          <>
            {' '}en <span className="font-medium text-carbon">{sesion.codigoEmpresa}</span>
          </>
        )}
      </p>

      <ul className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
        {accesos.map((item) => (
          <li key={item.ruta}>
            <Link
              to={item.ruta}
              className="group flex h-full flex-col gap-4 rounded-lg border border-borde bg-superficie p-6 transition-colors hover:border-texto-secundario"
            >
              <span className="grid size-10 place-items-center rounded-lg bg-piedra text-carbon">
                <item.icono tamano={20} />
              </span>
              <span className="flex-1">
                <span className="block font-titulo text-h3 font-semibold">{etiquetaPara(item, sesion.rol)}</span>
                <span className="mt-1 block text-sm text-texto-secundario">{item.descripcion}</span>
              </span>
              <span className="inline-flex items-center gap-1 text-sm font-medium text-oliva">
                Abrir
                <IconoFlechaDer tamano={16} className="transition-transform group-hover:translate-x-0.5" />
              </span>
            </Link>
          </li>
        ))}
      </ul>
    </>
  )
}
