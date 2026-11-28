import { leerErrorDto } from '../../../shared/api/extraerMensajeErrorBackend'

/**
 * Códigos que significan "otra decisión se adelantó": la salida es recargar la evaluación antes de decidir.
 * Otros 409 (EVIDENCIA_INVALIDA, EVALUACION_INCOMPLETA) se muestran junto a la acción: se resuelven ajustando.
 */
const CODIGOS_RECARGA = ['REVISION_DESACTUALIZADA', 'EVALUACION_YA_CONFIRMADA']

export function esConflictoDeRevision(error: unknown): boolean {
  const codigo = leerErrorDto(error)?.codigo
  return codigo !== undefined && CODIGOS_RECARGA.includes(codigo)
}
