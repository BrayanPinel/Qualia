// SuscripcionDto, PlanDto y ConsumoDto viven en shared/types/suscripciones.types.ts porque también los usan
// el registro de empresa y la plataforma. Aquí solo va lo propio del módulo.

/** Valores exactos que acepta PagoSimuladoValidator. */
export type ResultadoPagoSimulado = 'Aprobado' | 'Rechazado'

export const RESULTADOS_PAGO: readonly ResultadoPagoSimulado[] = ['Aprobado', 'Rechazado']

/** PagoSimuladoDto { resultado }. El backend rechaza cualquier otra propiedad. */
export interface PagoSimuladoDto {
  resultado: ResultadoPagoSimulado
}
