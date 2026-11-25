import { apiClient } from '../../../shared/api/apiClient'
import type { PaginaDto, Paginacion } from '../../../shared/types/comunes.types'
import type { SuscripcionDto } from '../../../shared/types/suscripciones.types'
import type { CambiarSuscripcionDto, EmpresaComercialDto } from '../types/plataforma.types'

// Rutas globales de PlatformAdmin: la empresa objetivo va en la ruta, no sale de la sesión.

/** GET /api/plataforma/empresas. */
export async function listarEmpresas(paginacion: Paginacion): Promise<PaginaDto<EmpresaComercialDto>> {
  const { data } = await apiClient.get<PaginaDto<EmpresaComercialDto>>('/plataforma/empresas', { params: paginacion })
  return data
}

/** PUT /api/plataforma/empresas/{empresaId}/suscripcion → 200 SuscripcionDto. */
export async function cambiarSuscripcion(empresaId: string, dto: CambiarSuscripcionDto): Promise<SuscripcionDto> {
  const { data } = await apiClient.put<SuscripcionDto>(`/plataforma/empresas/${empresaId}/suscripcion`, dto)
  return data
}
