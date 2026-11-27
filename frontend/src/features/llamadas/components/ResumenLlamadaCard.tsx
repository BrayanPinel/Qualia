import type { ReactNode } from 'react'
import { BadgeEstadoLlamada } from '../../../shared/components/estadosBadge'
import { IconoGiro } from '../../../shared/components/Iconos'
import { Tarjeta } from '../../../shared/components/Tarjeta'
import type { LlamadaDto } from '../types/llamada.types'
import { formatearFechaHora, formatearSegundos } from '../utils/tiempo'

interface PropsResumen {
  llamada: LlamadaDto
  nombreCampana: string
  nombreAgente: string | null
  enProceso: boolean
}

function Dato({ etiqueta, children }: { etiqueta: string; children: ReactNode }) {
  return (
    <div className="flex flex-col gap-1">
      <dt className="text-meta text-texto-secundario">{etiqueta}</dt>
      <dd className="text-sm font-medium">{children}</dd>
    </div>
  )
}

/** Datos de la llamada y su estado de procesamiento (se actualiza solo mientras está en proceso). */
export function ResumenLlamadaCard({ llamada, nombreCampana, nombreAgente, enProceso }: PropsResumen) {
  return (
    <Tarjeta>
      <dl className="grid grid-cols-2 gap-5 md:grid-cols-5">
        <Dato etiqueta="Estado">
          <BadgeEstadoLlamada estado={llamada.estado} />
        </Dato>
        <Dato etiqueta="Campaña">{nombreCampana}</Dato>
        {nombreAgente && <Dato etiqueta="Agente">{nombreAgente}</Dato>}
        <Dato etiqueta="Fecha">{formatearFechaHora(llamada.fechaLlamada)}</Dato>
        <Dato etiqueta="Duración">
          {formatearSegundos(llamada.duracionSeg)} · Rúbrica v{llamada.versionRubrica}
        </Dato>
      </dl>
      {enProceso && (
        <p role="status" className="mt-5 flex items-center gap-2 text-sm text-texto-secundario">
          <IconoGiro tamano={16} className="animate-spin text-oliva" />
          Procesando… el estado se actualiza automáticamente.
        </p>
      )}
      {llamada.motivoFallo && (
        <p role="alert" className="mt-5 rounded-lg border border-error/30 bg-error-fondo px-4 py-3 text-sm text-error">
          {llamada.motivoFallo}
        </p>
      )}
    </Tarjeta>
  )
}
