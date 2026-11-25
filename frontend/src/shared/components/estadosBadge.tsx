import type { EstadoDisputa, EstadoLlamada, Rol } from '../types/comunes.types'
import { ETIQUETA_ROL } from '../types/comunes.types'
import type { EstadoSuscripcion } from '../types/suscripciones.types'
import { ETIQUETA_ESTADO_SUSCRIPCION } from '../types/suscripciones.types'
import { Badge } from './Badge'
import { IconoCandado } from './Iconos'

// Badges por dominio para que todos los módulos muestren los mismos estados igual.
// Cuatro ejes que no se mezclan: procesamiento de la llamada, carácter oficial de la evaluación,
// cumplimiento de un criterio y estado de una disputa. "Oficial" no significa "Cumple".

const ETIQUETA_LLAMADA: Record<EstadoLlamada, string> = {
  Disponible: 'Disponible',
  EnCola: 'En cola',
  Transcribiendo: 'Transcribiendo',
  Transcrita: 'Transcrita',
  Evaluando: 'Evaluando',
  EvaluadaPorIA: 'Evaluada por IA',
  Fallida: 'Fallida',
}

const EN_PROCESO: EstadoLlamada[] = ['EnCola', 'Transcribiendo', 'Transcrita', 'Evaluando']

export function BadgeEstadoLlamada({ estado }: { estado: EstadoLlamada }) {
  if (estado === 'Fallida') return <Badge tono="error">{ETIQUETA_LLAMADA[estado]}</Badge>
  // Sin animación: una tabla llena de iconos girando distrae en jornadas largas.
  if (EN_PROCESO.includes(estado)) return <Badge tono="proceso">{ETIQUETA_LLAMADA[estado]}</Badge>
  return <Badge tono="neutro">{ETIQUETA_LLAMADA[estado]}</Badge>
}

/** Carácter de la evaluación: oficial (sin revisión pendiente) o por revisar. */
export function BadgeOficial({ requiereRevision }: { requiereRevision: boolean }) {
  return requiereRevision ? (
    <Badge tono="alerta">Por revisar</Badge>
  ) : (
    <Badge tono="neutro" icono={<IconoCandado tamano={14} />}>
      Oficial
    </Badge>
  )
}

export function BadgeCumplimiento({ cumple }: { cumple: boolean }) {
  return cumple ? <Badge tono="exito">Cumple</Badge> : <Badge tono="error">No cumple</Badge>
}

export function BadgeEstadoDisputa({ estado }: { estado: EstadoDisputa }) {
  return estado === 'Abierta' ? <Badge tono="alerta">Abierta</Badge> : <Badge tono="neutro">Resuelta</Badge>
}

export function BadgeActivo({ activo }: { activo: boolean }) {
  return activo ? <Badge tono="exito">Activo</Badge> : <Badge tono="neutro">Inactivo</Badge>
}

export function BadgeRol({ rol }: { rol: Rol }) {
  return (
    <Badge tono="neutro" sinIcono>
      {ETIQUETA_ROL[rol]}
    </Badge>
  )
}

const TONO_SUSCRIPCION = { Prueba: 'neutro', Activa: 'exito', EnMora: 'alerta', Vencida: 'error' } as const

export function BadgeEstadoSuscripcion({ estado }: { estado: EstadoSuscripcion }) {
  return <Badge tono={TONO_SUSCRIPCION[estado]}>{ETIQUETA_ESTADO_SUSCRIPCION[estado]}</Badge>
}
