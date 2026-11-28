import { apiClient } from '../../../shared/api/apiClient'
import { rutaEmpresa } from '../../../shared/api/rutaEmpresa'
import type { CrearRubricaDto, RubricaDto } from '../types/rubrica.types'

// Endpoints de rúbricas versionadas (AdminEmpresa).

/** GET {e}/campanas/{id}/rubricas: todas las versiones, de la más reciente a la más antigua. */
export async function listarRubricas(campanaId: string): Promise<RubricaDto[]> {
  const { data } = await apiClient.get<RubricaDto[]>(rutaEmpresa(`/campanas/${campanaId}/rubricas`))
  return data
}

/** No hay GET {e}/rubricas/{id}: se busca entre las versiones de la campaña. */
export async function obtenerRubrica(campanaId: string, rubricaId: string): Promise<RubricaDto> {
  const rubrica = (await listarRubricas(campanaId)).find((r) => r.id === rubricaId)
  if (!rubrica) throw new Error('La rúbrica no existe.')
  return rubrica
}

/** POST {e}/campanas/{id}/rubricas → 201 RubricaDto: nueva versión en Borrador (última versión + 1). */
export async function crearRubrica(campanaId: string, dto: CrearRubricaDto): Promise<RubricaDto> {
  const { data } = await apiClient.post<RubricaDto>(rutaEmpresa(`/campanas/${campanaId}/rubricas`), dto)
  return data
}

/** PUT {e}/rubricas/{id} → 204: reemplaza nombre y criterios. 409 RUBRICA_PUBLICADA si ya no es Borrador. */
export async function actualizarRubrica(rubricaId: string, dto: CrearRubricaDto): Promise<void> {
  await apiClient.put(rutaEmpresa(`/rubricas/${rubricaId}`), dto)
}

/** POST {e}/rubricas/{id}/publicar → RubricaDto Activa; la activa anterior pasa a Retirada. */
export async function publicarRubrica(rubricaId: string): Promise<RubricaDto> {
  const { data } = await apiClient.post<RubricaDto>(rutaEmpresa(`/rubricas/${rubricaId}/publicar`))
  return data
}
