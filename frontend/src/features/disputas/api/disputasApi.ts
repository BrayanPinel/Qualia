import { apiClient } from '../../../shared/api/apiClient'
import { rutaEmpresa } from '../../../shared/api/rutaEmpresa'
import type { PaginaDto } from '../../../shared/types/comunes.types'
import type { CrearDisputaDto, DisputaDto, FiltroDisputas, ResolverDisputaDto } from '../types/disputa.types'

/** POST {e}/evaluaciones/{id}/disputas → 201 (Agente dueño, evaluación oficial, plan con disputas). */
export async function crearDisputa(evaluacionId: string, dto: CrearDisputaDto): Promise<DisputaDto> {
  const { data } = await apiClient.post<DisputaDto>(rutaEmpresa(`/evaluaciones/${evaluacionId}/disputas`), dto)
  return data
}

/** GET {e}/disputas: Admin todas; Supervisor las de sus campañas; Agente las suyas. Más recientes primero. */
export async function listarDisputas(filtro: FiltroDisputas): Promise<PaginaDto<DisputaDto>> {
  const { data } = await apiClient.get<PaginaDto<DisputaDto>>(rutaEmpresa('/disputas'), { params: filtro })
  return data
}

/** GET {e}/disputas/{id}: 403 DISPUTA_FUERA_DE_ALCANCE si es de la empresa pero no del rol; 404 si no existe. */
export async function obtenerDisputa(id: string): Promise<DisputaDto> {
  const { data } = await apiClient.get<DisputaDto>(rutaEmpresa(`/disputas/${id}`))
  return data
}

/** PUT {e}/disputas/{id}/resolucion (Supervisor asignado). 409 DISPUTA_YA_RESUELTA o REVISION_DESACTUALIZADA. */
export async function resolverDisputa(id: string, dto: ResolverDisputaDto): Promise<DisputaDto> {
  const { data } = await apiClient.put<DisputaDto>(rutaEmpresa(`/disputas/${id}/resolucion`), dto)
  return data
}
