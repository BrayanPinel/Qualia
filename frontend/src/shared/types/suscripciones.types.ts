// DTOs de planes y suscripciones que usan varios módulos (registro, plataforma y suscripción).

export type EstadoSuscripcion = 'Prueba' | 'Activa' | 'EnMora' | 'Vencida'

export const ESTADOS_SUSCRIPCION: readonly EstadoSuscripcion[] = ['Prueba', 'Activa', 'EnMora', 'Vencida']

export const ETIQUETA_ESTADO_SUSCRIPCION: Record<EstadoSuscripcion, string> = {
  Prueba: 'Prueba',
  Activa: 'Activa',
  EnMora: 'En mora',
  Vencida: 'Vencida',
}

/** Códigos de Funcion_plan (CodigosFuncionPlan en el backend). */
export type CodigoFuncionPlan = 'EVALUACION' | 'SCORECARD_BASICO' | 'TENDENCIA' | 'COMPARACION_CAMPANAS' | 'DISPUTAS'

export const ETIQUETA_FUNCION_PLAN: Record<CodigoFuncionPlan, string> = {
  EVALUACION: 'Evaluación de llamadas',
  SCORECARD_BASICO: 'Scorecard individual',
  TENDENCIA: 'Tendencia mensual',
  COMPARACION_CAMPANAS: 'Comparación por campaña',
  DISPUTAS: 'Disputas de evaluaciones',
}

/** PlanDto. limiteSupervisores y limiteRubricas en null significan "sin límite". */
export interface PlanDto {
  id: string
  nombre: string
  precioMensual: number
  minutosIncluidos: number
  limiteSupervisores: number | null
  limiteRubricas: number | null
  precioExcedente: number
  funciones: string[]
}

export interface PeriodoConsumoDto {
  id: string
  periodo: string
  fechaInicio: string
  fechaFin: string
  minutosContratados: number
}

export interface SuscripcionDto {
  id: string
  empresaId: string
  planId: string
  estado: EstadoSuscripcion
  fechaVencimiento: string
  permiteExcedente: boolean
  periodoActual?: PeriodoConsumoDto | null
}

/** ConsumoDto. periodo con formato YYYY-MM. */
export interface ConsumoDto {
  empresaId: string
  periodo: string
  minutosContratados: number
  minutosReservados: number
  minutosFacturables: number
  minutosDisponibles: number
  minutosExcedentes: number
}
