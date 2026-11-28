import { Badge } from '../../../shared/components/Badge'
import { IconoCandado } from '../../../shared/components/Iconos'
import type { EstadoRubrica } from '../../../shared/types/comunes.types'

// Estado de la campaña y de su rúbrica, iguales en la tabla, el detalle y el editor.

/** Borrador editable; Activa y Retirada ya publicadas e inmutables (candado). */
export function BadgeEstadoRubrica({ estado }: { estado: EstadoRubrica }) {
  if (estado === 'Borrador') return <Badge tono="alerta">Borrador</Badge>
  if (estado === 'Activa') return <Badge tono="exito" icono={<IconoCandado tamano={14} />}>Activa</Badge>
  return <Badge tono="neutro" icono={<IconoCandado tamano={14} />}>Retirada</Badge>
}

export function BadgeEstadoCampana({ activa }: { activa: boolean }) {
  return activa ? <Badge tono="exito">Activa</Badge> : <Badge tono="neutro">Inactiva</Badge>
}

/** Sin rúbrica activa no se pueden cargar ni evaluar llamadas de la campaña. */
export function BadgeRubricaActiva({ rubricaActivaId }: { rubricaActivaId: string | null }) {
  return rubricaActivaId ? <Badge tono="neutro">Rúbrica publicada</Badge> : <Badge tono="alerta">Sin rúbrica activa</Badge>
}
