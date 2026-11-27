import { EncabezadoPagina } from '../../../shared/components/EncabezadoPagina'
import { useSesion } from '../../auth/context/useSesion'
import { MiDesempenoView } from './MiDesempenoView'
import { PanelFiltroDesempeno } from './PanelFiltroDesempeno'
import { ScorecardsEquipo } from './ScorecardsEquipo'

/** Desempeño: el Agente ve su propio scorecard; AdminEmpresa y Supervisor, los del equipo de una campaña. */
export function DesempenoView() {
  const { sesion } = useSesion()
  if (!sesion) return null
  if (sesion.rol === 'Agente') return <MiDesempenoView />

  return (
    <>
      <EncabezadoPagina
        titulo="Desempeño"
        descripcion="Scorecards por agente: promedio general y por criterio sobre las evaluaciones oficiales sin disputas abiertas."
      />
      <PanelFiltroDesempeno>
        {(filtro, campana) => (
          <ScorecardsEquipo key={`${filtro.campanaId}-${filtro.periodo}`} filtro={filtro} campana={campana} rol={sesion.rol} />
        )}
      </PanelFiltroDesempeno>
    </>
  )
}
