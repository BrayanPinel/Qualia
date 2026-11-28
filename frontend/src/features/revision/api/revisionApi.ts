import { apiClient } from '../../../shared/api/apiClient'
import { rutaEmpresa } from '../../../shared/api/rutaEmpresa'
import { obtenerEvaluacion, obtenerTranscripcion } from '../../llamadas/api/llamadasApi'
import type { EvaluacionDto, SegmentoDto } from '../../llamadas/types/llamada.types'
import type { AjustarEvaluacionDto } from '../types/revision.types'

// Decisión del Supervisor asignado sobre la propuesta de la IA. Ambas responden con la evaluación vigente.

export interface DatosRevision {
  evaluacion: EvaluacionDto
  segmentos: SegmentoDto[]
}

/** Evaluación y transcripción de la llamada (endpoints del módulo de llamadas, por llamadaId). */
export async function cargarDatosRevision(llamadaId: string): Promise<DatosRevision> {
  const [evaluacion, segmentos] = await Promise.all([obtenerEvaluacion(llamadaId), obtenerTranscripcion(llamadaId)])
  return { evaluacion, segmentos }
}

/** POST {e}/evaluaciones/{id}/confirmar. 409 REVISION_DESACTUALIZADA si otra revisión se adelantó. */
export async function confirmarEvaluacion(evaluacionId: string, revisionEsperada: number): Promise<EvaluacionDto> {
  const { data } = await apiClient.post<EvaluacionDto>(rutaEmpresa(`/evaluaciones/${evaluacionId}/confirmar`), { revisionEsperada })
  return data
}

/** PUT {e}/evaluaciones/{id}/ajuste: ajuste completo; conserva el resultado de la IA. */
export async function ajustarEvaluacion(evaluacionId: string, dto: AjustarEvaluacionDto): Promise<EvaluacionDto> {
  const { data } = await apiClient.put<EvaluacionDto>(rutaEmpresa(`/evaluaciones/${evaluacionId}/ajuste`), dto)
  return data
}
