import type { ComponentType } from 'react'
import {
  IconoDisputa,
  IconoDocumento,
  IconoEdificio,
  IconoGrafica,
  IconoInicio,
  IconoLlamada,
  IconoRevision,
  IconoRubrica,
  IconoTarjeta,
  IconoUsuarios,
} from '../shared/components/Iconos'
import type { Rol } from '../shared/types/comunes.types'

// Menú según el rol. Lo usan el sidebar y las tarjetas de inicio, así que hay una sola lista.
// Los permisos reales los decide el backend; esto solo evita mostrar lo que el rol no puede usar.

export interface ItemNavegacion {
  ruta: string
  etiqueta: string
  /** Etiqueta distinta para un rol concreto (el Agente ve "Mis llamadas"). */
  etiquetaPorRol?: Partial<Record<Rol, string>>
  descripcion: string
  icono: ComponentType<{ tamano?: number }>
  roles: readonly Rol[]
}

export const NAVEGACION: readonly ItemNavegacion[] = [
  { ruta: '/inicio', etiqueta: 'Inicio', descripcion: '', icono: IconoInicio, roles: ['AdminEmpresa', 'Supervisor', 'Agente', 'Auditor'] },
  {
    ruta: '/llamadas',
    etiqueta: 'Llamadas',
    etiquetaPorRol: { Agente: 'Mis llamadas' },
    descripcion: 'Estado del procesamiento, transcripción y evaluación de cada llamada.',
    icono: IconoLlamada,
    roles: ['AdminEmpresa', 'Supervisor', 'Agente'],
  },
  {
    ruta: '/revision',
    etiqueta: 'Revisión de calidad',
    descripcion: 'Evaluaciones que la IA marcó para revisión humana: confirmar o ajustar.',
    icono: IconoRevision,
    roles: ['Supervisor'],
  },
  {
    ruta: '/campanas',
    etiqueta: 'Campañas y rúbricas',
    descripcion: 'Campañas, asignación de agentes y supervisores, y versiones de rúbrica.',
    icono: IconoRubrica,
    roles: ['AdminEmpresa'],
  },
  {
    ruta: '/disputas',
    etiqueta: 'Disputas',
    descripcion: 'Disputas abiertas por los agentes y su resolución.',
    icono: IconoDisputa,
    roles: ['AdminEmpresa', 'Supervisor'],
  },
  {
    ruta: '/desempeno',
    etiqueta: 'Desempeño',
    etiquetaPorRol: { Agente: 'Mi desempeño' },
    descripcion: 'Scorecards con el promedio general y por criterio.',
    icono: IconoGrafica,
    roles: ['AdminEmpresa', 'Supervisor', 'Agente'],
  },
  {
    ruta: '/reportes',
    etiqueta: 'Reportes',
    descripcion: 'Indicadores agregados por campaña y periodo.',
    icono: IconoDocumento,
    roles: ['AdminEmpresa', 'Supervisor', 'Auditor'],
  },
  {
    ruta: '/usuarios',
    etiqueta: 'Usuarios',
    descripcion: 'Cuentas de la empresa, roles y estado.',
    icono: IconoUsuarios,
    roles: ['AdminEmpresa'],
  },
  {
    ruta: '/suscripcion',
    etiqueta: 'Suscripción',
    descripcion: 'Plan, vigencia, pago simulado y consumo de minutos.',
    icono: IconoTarjeta,
    roles: ['AdminEmpresa'],
  },
  {
    ruta: '/plataforma/empresas',
    etiqueta: 'Empresas',
    descripcion: 'Empresas registradas, su plan, estado y consumo.',
    icono: IconoEdificio,
    roles: ['PlatformAdmin'],
  },
]

export function menuPorRol(rol: Rol): ItemNavegacion[] {
  return NAVEGACION.filter((item) => item.roles.includes(rol))
}

export function etiquetaPara(item: ItemNavegacion, rol: Rol): string {
  return item.etiquetaPorRol?.[rol] ?? item.etiqueta
}

export function rutaInicioPorRol(rol: Rol): string {
  return rol === 'PlatformAdmin' ? '/plataforma/empresas' : '/inicio'
}
