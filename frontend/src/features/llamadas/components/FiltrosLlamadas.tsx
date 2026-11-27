import { CampoSelector } from '../../../shared/components/Formulario'
import type { CampanaDto } from '../../../shared/types/campanas.types'
import type { EstadoLlamada } from '../../../shared/types/comunes.types'
import { ESTADOS_LLAMADA, ETIQUETA_ESTADO_LLAMADA, type AgenteOpcion, type SeleccionFiltros } from '../types/llamada.types'

interface PropsFiltros {
  valor: SeleccionFiltros
  campanas: CampanaDto[]
  /** Solo el AdminEmpresa puede filtrar por agente (es quien puede ver los nombres). */
  agentes: AgenteOpcion[]
  ocultarEstado?: boolean
  onCambiar: (valor: SeleccionFiltros) => void
}

/** Filtros de la lista. Son listas cerradas que vienen del backend, por eso no requieren validación. */
export function FiltrosLlamadas({ valor, campanas, agentes, ocultarEstado = false, onCambiar }: PropsFiltros) {
  return (
    <div className="grid gap-4 px-6 pt-6 pb-4 sm:grid-cols-3">
      <CampoSelector
        etiqueta="Campaña"
        value={valor.campanaId}
        onChange={(e) => onCambiar({ ...valor, campanaId: e.target.value })}
      >
        <option value="">Todas las campañas</option>
        {campanas.map((c) => (
          <option key={c.id} value={c.id}>
            {c.nombre}
          </option>
        ))}
      </CampoSelector>

      {agentes.length > 0 && (
        <CampoSelector
          etiqueta="Agente"
          value={valor.agenteId}
          onChange={(e) => onCambiar({ ...valor, agenteId: e.target.value })}
        >
          <option value="">Todos los agentes</option>
          {agentes.map((a) => (
            <option key={a.id} value={a.id}>
              {a.nombre}
            </option>
          ))}
        </CampoSelector>
      )}

      {!ocultarEstado && (
        <CampoSelector
          etiqueta="Estado"
          value={valor.estado}
          onChange={(e) => onCambiar({ ...valor, estado: e.target.value as EstadoLlamada | '' })}
        >
          <option value="">Todos los estados</option>
          {ESTADOS_LLAMADA.map((estado) => (
            <option key={estado} value={estado}>
              {ETIQUETA_ESTADO_LLAMADA[estado]}
            </option>
          ))}
        </CampoSelector>
      )}
    </div>
  )
}
