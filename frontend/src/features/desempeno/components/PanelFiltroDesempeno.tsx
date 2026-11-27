import { useState } from 'react'
import type { ReactNode } from 'react'
import { listarCampanasVisibles } from '../../../shared/api/campanasVisiblesApi'
import { EstadoCarga, EstadoError, EstadoVacio } from '../../../shared/components/EstadosVista'
import { Tarjeta } from '../../../shared/components/Tarjeta'
import { useConsulta } from '../../../shared/hooks/useConsulta'
import { periodoActual } from '../schemas/filtroDesempeno.schema'
import type { FiltroDesempeno } from '../types/desempeno.types'
import { FiltroDesempenoForm } from './FiltroDesempenoForm'

interface PropsPanel {
  /** Contenido que depende del filtro elegido (scorecard, equipo o reporte). */
  children: (filtro: FiltroDesempeno, nombreCampana: string) => ReactNode
}

/** Carga las campañas visibles, muestra el filtro y entrega el filtro vigente al contenido. */
export function PanelFiltroDesempeno({ children }: PropsPanel) {
  const campanas = useConsulta('desempeno-campanas', listarCampanasVisibles)
  const [elegido, setElegido] = useState<FiltroDesempeno | null>(null)

  if (campanas.error) return <EstadoError mensaje={campanas.error} onReintentar={campanas.recargar} />
  if (!campanas.datos) return <EstadoCarga texto="Cargando campañas…" />
  if (campanas.datos.length === 0) {
    return (
      <Tarjeta>
        <EstadoVacio titulo="No tienes campañas" descripcion="El desempeño se calcula por campaña. Pide que te asignen a una." />
      </Tarjeta>
    )
  }

  const filtro = elegido ?? { campanaId: campanas.datos[0].id, periodo: periodoActual() }
  const nombre = campanas.datos.find((c) => c.id === filtro.campanaId)?.nombre ?? ''

  return (
    <div className="flex flex-col gap-6">
      <Tarjeta>
        <FiltroDesempenoForm campanas={campanas.datos} valor={filtro} consultando={false} onConsultar={setElegido} />
      </Tarjeta>
      {children(filtro, nombre)}
    </div>
  )
}
