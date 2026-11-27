import { LlamadasView } from './LlamadasView'

/** Bandeja del Supervisor: llamadas con requiereRevision=true. Cada fila enlaza a la revisión humana (/revision/:llamadaId). */
export function BandejaRevisionView() {
  return <LlamadasView bandejaRevision />
}
