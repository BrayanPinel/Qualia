import { listarCampanasVisibles } from '../../../shared/api/campanasVisiblesApi'
import { useConsulta } from '../../../shared/hooks/useConsulta'
import type { Rol } from '../../../shared/types/comunes.types'
import { listarAgentesEmpresa } from '../api/llamadasApi'
import { idCorto } from '../utils/tiempo'

/**
 * Campañas visibles y, para el AdminEmpresa, los agentes de la empresa: con ellos la tabla muestra nombres
 * en lugar de identificadores. LlamadaDto solo trae campanaId y agenteId.
 */
export function useCatalogosLlamadas(rol: Rol) {
  const esAdmin = rol === 'AdminEmpresa'
  const campanas = useConsulta('llamadas-campanas', listarCampanasVisibles)
  const agentes = useConsulta(`llamadas-agentes-${esAdmin}`, () => (esAdmin ? listarAgentesEmpresa() : Promise.resolve([])))

  const nombreCampana = (id: string) => campanas.datos?.find((c) => c.id === id)?.nombre ?? idCorto(id)
  const nombreAgente = (id: string) => agentes.datos?.find((a) => a.id === id)?.nombre ?? `Agente ${idCorto(id)}`

  return {
    campanas: campanas.datos ?? [],
    agentes: agentes.datos ?? [],
    nombreCampana,
    /** El Agente solo ve sus llamadas: no necesita la columna. */
    nombreAgente: rol === 'Agente' ? null : nombreAgente,
  }
}
