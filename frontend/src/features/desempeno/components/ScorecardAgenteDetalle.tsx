import { EstadoCarga, EstadoError } from '../../../shared/components/EstadosVista'
import { useConsulta } from '../../../shared/hooks/useConsulta'
import { obtenerScorecardAgente } from '../api/desempenoApi'
import type { FiltroDesempeno } from '../types/desempeno.types'
import { ScorecardDetalle } from './ScorecardDetalle'

/** Scorecard individual de un agente, pedido a GET {e}/agentes/{id}/scorecard. */
export function ScorecardAgenteDetalle({ agenteId, filtro }: { agenteId: string; filtro: FiltroDesempeno }) {
  const { datos, error, recargar } = useConsulta(`scorecard-${agenteId}-${filtro.campanaId}-${filtro.periodo}`, () =>
    obtenerScorecardAgente(agenteId, filtro),
  )

  if (error) return <EstadoError mensaje={error} onReintentar={recargar} />
  if (!datos) return <EstadoCarga texto="Cargando scorecard…" />
  return <ScorecardDetalle scorecard={datos} />
}
