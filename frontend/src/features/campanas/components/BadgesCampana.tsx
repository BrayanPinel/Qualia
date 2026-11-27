import { Badge } from '../../../shared/components/Badge'

// Estado de la campaña y de su rúbrica, iguales en la tabla y en el detalle.

export function BadgeEstadoCampana({ activa }: { activa: boolean }) {
  return activa ? <Badge tono="exito">Activa</Badge> : <Badge tono="neutro">Inactiva</Badge>
}

/** Sin rúbrica activa no se pueden cargar ni evaluar llamadas de la campaña. */
export function BadgeRubricaActiva({ rubricaActivaId }: { rubricaActivaId: string | null }) {
  return rubricaActivaId ? <Badge tono="neutro">Rúbrica publicada</Badge> : <Badge tono="alerta">Sin rúbrica activa</Badge>
}
