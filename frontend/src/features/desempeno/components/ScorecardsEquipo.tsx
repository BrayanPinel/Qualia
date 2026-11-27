import { useState } from 'react'
import { Dialogo } from '../../../shared/components/Dialogo'
import { EstadoCarga, EstadoError, EstadoVacio } from '../../../shared/components/EstadosVista'
import { Paginador } from '../../../shared/components/Paginador'
import { Tarjeta } from '../../../shared/components/Tarjeta'
import { useConsulta } from '../../../shared/hooks/useConsulta'
import type { Rol } from '../../../shared/types/comunes.types'
import { listarNombresAgentes, listarScorecardsCampana } from '../api/desempenoApi'
import type { FiltroDesempeno, ScorecardDto } from '../types/desempeno.types'
import { ScorecardAgenteDetalle } from './ScorecardAgenteDetalle'
import { TablaScorecards } from './TablaScorecards'

const TAMANO_PAGINA = 10

interface PropsEquipo {
  filtro: FiltroDesempeno
  campana: string
  rol: Rol
}

/** Scorecards de los agentes de una campaña (AdminEmpresa o Supervisor asignado), paginados. */
export function ScorecardsEquipo({ filtro, campana, rol }: PropsEquipo) {
  const [pagina, setPagina] = useState(1)
  const [viendo, setViendo] = useState<ScorecardDto | null>(null)
  const clave = `${filtro.campanaId}-${filtro.periodo}`

  const { datos, error, cargando, recargar } = useConsulta(`scorecards-${clave}-${pagina}`, () =>
    listarScorecardsCampana(filtro, { pagina, tamanoPagina: TAMANO_PAGINA }),
  )
  // Solo el AdminEmpresa puede consultar los nombres; el Supervisor ve el identificador del agente.
  const nombres = useConsulta(`nombres-agentes-${filtro.campanaId}-${rol}`, () =>
    rol === 'AdminEmpresa' ? listarNombresAgentes(filtro.campanaId) : Promise.resolve([]),
  )
  const nombreAgente = (id: string) => nombres.datos?.find((n) => n.id === id)?.nombre ?? `Agente ${id.slice(-8)}`

  return (
    <Tarjeta titulo={campana} descripcion={`Equipo · periodo ${filtro.periodo}`} sinRelleno>
      {error && <EstadoError mensaje={error} onReintentar={recargar} />}
      {!error && !datos && cargando && <EstadoCarga texto="Calculando scorecards…" />}
      {!error && datos && datos.total === 0 && (
        <EstadoVacio titulo="Sin agentes" descripcion="La campaña no tiene agentes asignados ni llamadas evaluadas en el periodo." />
      )}
      {!error && datos && datos.total > 0 && (
        <div aria-busy={cargando}>
          <TablaScorecards scorecards={datos.items} nombreAgente={nombreAgente} onVer={setViendo} />
          <Paginador
            pagina={datos.pagina}
            tamanoPagina={datos.tamanoPagina}
            total={datos.total}
            onCambiar={setPagina}
            deshabilitado={cargando}
          />
        </div>
      )}

      <Dialogo
        abierto={viendo !== null}
        titulo={viendo ? nombreAgente(viendo.agenteId) : ''}
        descripcion={`${campana} · ${filtro.periodo}`}
        onCerrar={() => setViendo(null)}
      >
        {viendo && <ScorecardAgenteDetalle agenteId={viendo.agenteId} filtro={filtro} />}
      </Dialogo>
    </Tarjeta>
  )
}
