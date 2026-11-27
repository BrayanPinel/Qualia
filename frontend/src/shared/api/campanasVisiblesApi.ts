import { apiClient } from './apiClient'
import { rutaEmpresa } from './rutaEmpresa'
import type { CampanaDto } from '../types/campanas.types'
import type { PaginaDto } from '../types/comunes.types'

/**
 * GET {e}/campanas con el tamaño máximo de página: campañas que el usuario puede ver
 * (AdminEmpresa y Auditor todas; Supervisor y Agente las asignadas). Alimenta filtros y selectores.
 */
export async function listarCampanasVisibles(): Promise<CampanaDto[]> {
  const { data } = await apiClient.get<PaginaDto<CampanaDto>>(rutaEmpresa('/campanas'), {
    params: { pagina: 1, tamanoPagina: 100 },
  })
  return data.items
}
