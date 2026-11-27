// Espejo exacto de DesempenoDtos.cs (camelCase). periodo con formato YYYY-MM.

export interface PromedioCriterioDto {
  rubricaId: string
  version: number
  criterioId: string
  nombre: string
  promedio: number
  cantidad: number
}

/**
 * ScorecardDto. puntajePromedio es null si no hay muestra suficiente; tendencia es null si el plan
 * no incluye TENDENCIA o no hay datos del mes anterior; promedioCampana es null sin COMPARACION_CAMPANAS.
 */
export interface ScorecardDto {
  empresaId: string
  agenteId: string
  campanaId: string
  periodo: string
  llamadasEvaluadas: number
  llamadasConfirmadas: number
  llamadasElegibles: number
  muestraSuficiente: boolean
  puntajePromedio: number | null
  tendencia: number | null
  promedioCampana: number | null
  criticosFallidos: number
  criterios: PromedioCriterioDto[]
}

export interface ReporteCampanaDto {
  empresaId: string
  campanaId: string
  periodo: string
  llamadasElegibles: number
  muestraSuficiente: boolean
  promedio: number | null
  criticosFallidos: number
}

/** Query de los scorecards (FiltroScorecardDto) y del reporte (FiltroReporteDto). */
export interface FiltroDesempeno {
  campanaId: string
  periodo: string
}

export interface NombreAgente {
  id: string
  nombre: string
}
