import { EncabezadoPagina } from '../../../shared/components/EncabezadoPagina'
import { EstadoCarga, EstadoError } from '../../../shared/components/EstadosVista'
import { Tarjeta } from '../../../shared/components/Tarjeta'
import { useConsulta } from '../../../shared/hooks/useConsulta'
import { obtenerMiScorecard } from '../api/desempenoApi'
import type { FiltroDesempeno } from '../types/desempeno.types'
import { PanelFiltroDesempeno } from './PanelFiltroDesempeno'
import { ScorecardDetalle } from './ScorecardDetalle'

function MiScorecard({ filtro, campana }: { filtro: FiltroDesempeno; campana: string }) {
  const { datos, error, recargar } = useConsulta(`mi-scorecard-${filtro.campanaId}-${filtro.periodo}`, () =>
    obtenerMiScorecard(filtro),
  )

  return (
    <Tarjeta titulo={campana} descripcion={`Periodo ${datos?.periodo ?? filtro.periodo}`}>
      {error && <EstadoError mensaje={error} onReintentar={recargar} />}
      {!error && !datos && <EstadoCarga texto="Calculando tu scorecard…" />}
      {!error && datos && <ScorecardDetalle scorecard={datos} />}
    </Tarjeta>
  )
}

/** "Mi desempeño" del Agente: promedio general y por criterio en una campaña y mes. */
export function MiDesempenoView() {
  return (
    <>
      <EncabezadoPagina
        titulo="Mi desempeño"
        descripcion="Solo cuentan las evaluaciones oficiales y sin disputas abiertas."
      />
      <PanelFiltroDesempeno>{(filtro, campana) => <MiScorecard filtro={filtro} campana={campana} />}</PanelFiltroDesempeno>
    </>
  )
}
