import type { EstadoDisputa } from '../../../shared/types/comunes.types'
import type { AjusteCriterioDto } from '../../revision/types/revision.types'

// Espejo de DisputasDtos.cs. llamadaId permite pedir la evaluación y la transcripción (se consultan por llamada).

export interface DisputaDto {
  id: string
  empresaId: string
  evaluacionId: string
  llamadaId: string
  estado: EstadoDisputa
  motivo: string
  resolucion: string | null
  resueltaPorId: string | null
  creadaEn: string
  resueltaEn: string | null
}

/** CrearDisputaDto { motivo }. */
export interface CrearDisputaDto {
  motivo: string
}

export type DecisionDisputa = 'Mantener' | 'Modificar'

/** ResolverDisputaDto: "Modificar" exige el ajuste completo de criterios; "Mantener" no admite criterios. */
export interface ResolverDisputaDto {
  decision: DecisionDisputa
  resolucion: string
  revisionEsperada: number
  criterios?: AjusteCriterioDto[]
}

/** Query de GET {e}/disputas (FiltroDisputasDto). */
export interface FiltroDisputas {
  pagina: number
  tamanoPagina: number
  estado?: EstadoDisputa
}

export const ESTADOS_DISPUTA: readonly EstadoDisputa[] = ['Abierta', 'Resuelta']
