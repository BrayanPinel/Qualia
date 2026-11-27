// La salida es CampanaDto (shared/types): la usan también llamadas y desempeño como selector.
export type { CampanaDto } from '../../../shared/types/campanas.types'

/** GuardarCampanaDto { nombre, activa }: el mismo cuerpo para crear (POST) y editar (PUT). */
export interface GuardarCampanaDto {
  nombre: string
  activa: boolean
}

/** Roles que se pueden asignar a una campaña (UsuarioCampana.RolAsignable). */
export const ROLES_ASIGNABLES = ['Agente', 'Supervisor'] as const
