import { apiClient } from '../../../shared/api/apiClient'
import { rutaEmpresa } from '../../../shared/api/rutaEmpresa'
import type { PaginaDto, Paginacion, UsuarioDto } from '../../../shared/types/comunes.types'
import type { FiltroDesempeno, NombreAgente, ReporteCampanaDto, ScorecardDto } from '../types/desempeno.types'

/** GET {e}/agentes/me/scorecard?campanaId&periodo (solo Agente). */
export async function obtenerMiScorecard(filtro: FiltroDesempeno): Promise<ScorecardDto> {
  const { data } = await apiClient.get<ScorecardDto>(rutaEmpresa('/agentes/me/scorecard'), { params: filtro })
  return data
}

/** GET {e}/agentes/{id}/scorecard?campanaId&periodo (AdminEmpresa o Supervisor asignado). */
export async function obtenerScorecardAgente(agenteId: string, filtro: FiltroDesempeno): Promise<ScorecardDto> {
  const { data } = await apiClient.get<ScorecardDto>(rutaEmpresa(`/agentes/${agenteId}/scorecard`), { params: filtro })
  return data
}

/** GET {e}/campanas/{id}/scorecards?pagina&tamanoPagina&periodo: un scorecard por agente, por nombre. */
export async function listarScorecardsCampana(
  filtro: FiltroDesempeno,
  paginacion: Paginacion,
): Promise<PaginaDto<ScorecardDto>> {
  const { data } = await apiClient.get<PaginaDto<ScorecardDto>>(rutaEmpresa(`/campanas/${filtro.campanaId}/scorecards`), {
    params: { ...paginacion, periodo: filtro.periodo },
  })
  return data
}

/** GET {e}/reportes/campanas/{id}?periodo (AdminEmpresa, Supervisor asignado o Auditor). */
export async function obtenerReporteCampana(filtro: FiltroDesempeno): Promise<ReporteCampanaDto> {
  const { data } = await apiClient.get<ReporteCampanaDto>(rutaEmpresa(`/reportes/campanas/${filtro.campanaId}`), {
    params: { periodo: filtro.periodo },
  })
  return data
}

/** GET {e}/campanas/{id}/usuarios (solo AdminEmpresa): nombres de los agentes para la tabla del equipo. */
export async function listarNombresAgentes(campanaId: string): Promise<NombreAgente[]> {
  const { data } = await apiClient.get<PaginaDto<UsuarioDto>>(rutaEmpresa(`/campanas/${campanaId}/usuarios`), {
    params: { pagina: 1, tamanoPagina: 100 },
  })
  return data.items.filter((u) => u.rol === 'Agente').map((u) => ({ id: u.id, nombre: u.nombre }))
}
