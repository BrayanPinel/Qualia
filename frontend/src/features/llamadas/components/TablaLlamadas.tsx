import { Link } from 'react-router-dom'
import { BadgeEstadoLlamada, BadgeOficial } from '../../../shared/components/estadosBadge'
import { Tabla, type ColumnaTabla } from '../../../shared/components/Tabla'
import type { LlamadaDto } from '../types/llamada.types'
import { CLASE_ENLACE_ACCION } from '../utils/estilos'
import { formatearFechaHora, formatearSegundos, idCorto } from '../utils/tiempo'

interface PropsTablaLlamadas {
  llamadas: LlamadaDto[]
  nombreCampana: (id: string) => string
  /** null: el rol no ve la columna de agente (el Agente solo ve sus propias llamadas). */
  nombreAgente: ((id: string) => string) | null
  /** En la bandeja de revisión se enlaza a la pantalla de revisión humana. */
  bandejaRevision: boolean
}

function EstadoConMotivo({ llamada }: { llamada: LlamadaDto }) {
  return (
    <div className="flex flex-col items-start gap-1">
      <BadgeEstadoLlamada estado={llamada.estado} />
      {llamada.motivoFallo && (
        <span className="max-w-64 truncate text-meta text-error" title={llamada.motivoFallo}>
          {llamada.motivoFallo}
        </span>
      )}
    </div>
  )
}

export function TablaLlamadas({ llamadas, nombreCampana, nombreAgente, bandejaRevision }: PropsTablaLlamadas) {
  const columnas: ColumnaTabla<LlamadaDto>[] = [
    { clave: 'fecha', encabezado: 'Fecha', celda: (l) => <span className="tabular-nums">{formatearFechaHora(l.fechaLlamada)}</span> },
    { clave: 'campana', encabezado: 'Campaña', celda: (l) => <span className="font-medium">{nombreCampana(l.campanaId)}</span> },
    ...(nombreAgente
      ? [{ clave: 'agente', encabezado: 'Agente', celda: (l: LlamadaDto) => nombreAgente(l.agenteId), ocultarEnMovil: true }]
      : []),
    { clave: 'duracion', encabezado: 'Duración', alinear: 'derecha', celda: (l) => formatearSegundos(l.duracionSeg), ocultarEnMovil: true },
    { clave: 'estado', encabezado: 'Estado', celda: (l) => <EstadoConMotivo llamada={l} /> },
    {
      clave: 'evaluacion',
      encabezado: 'Evaluación',
      celda: (l) =>
        l.requiereRevision === null ? <span className="text-texto-secundario">—</span> : <BadgeOficial requiereRevision={l.requiereRevision} />,
    },
    {
      clave: 'acciones',
      encabezado: 'Acciones',
      alinear: 'derecha',
      celda: (l) => (
        <div className="flex justify-end gap-1">
          {bandejaRevision && (
            <Link to={`/revision/${l.id}`} className={CLASE_ENLACE_ACCION}>
              Revisar
            </Link>
          )}
          <Link to={`/llamadas/${l.id}`} className={CLASE_ENLACE_ACCION} aria-label={`Ver detalle de la llamada ${idCorto(l.id)}`}>
            Ver detalle
          </Link>
        </div>
      ),
    },
  ]

  return <Tabla descripcion="Llamadas de la empresa" columnas={columnas} filas={llamadas} claveFila={(l) => l.id} />
}
