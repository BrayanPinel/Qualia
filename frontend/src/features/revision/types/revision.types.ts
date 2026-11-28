// Entrada de la revisión humana (RevisionDtos.cs). La salida es EvaluacionDto del módulo de llamadas.

/** ConfirmarEvaluacionDto { revisionEsperada }: la revisión que el supervisor tenía en pantalla. */
export interface ConfirmarEvaluacionDto {
  revisionEsperada: number
}

export interface EvidenciaEntradaDto {
  segmentoId: string
  citaTextual: string
}

/** Un criterio del ajuste. evidencias vacía = ausencia explícita de evidencia, explicada en la justificación (RN-10). */
export interface AjusteCriterioDto {
  criterioId: string
  puntaje: number
  justificacion: string
  evidencias: EvidenciaEntradaDto[]
}

/** Ajuste COMPLETO: todos los criterios de la rúbrica de la llamada, cada uno una vez. */
export interface AjustarEvaluacionDto {
  revisionEsperada: number
  motivo: string
  criterios: AjusteCriterioDto[]
}

/** ConstructorEvaluacion.LongitudMaximaCita. */
export const LONGITUD_MAXIMA_CITA = 1000
