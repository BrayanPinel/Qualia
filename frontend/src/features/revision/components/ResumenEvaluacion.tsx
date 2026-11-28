import { Badge } from '../../../shared/components/Badge'
import { BadgeCumplimiento, BadgeOficial } from '../../../shared/components/estadosBadge'
import { Tabla, type ColumnaTabla } from '../../../shared/components/Tabla'
import { Tarjeta } from '../../../shared/components/Tarjeta'
import { formatearNumero } from '../../../shared/utils/formato'
import { ETIQUETA_ESTADO_EVALUACION, type EvaluacionDto, type ResultadoCriterioDto } from '../../llamadas/types/llamada.types'

/** Propuesta actual: puntaje total, carácter oficial, revisión y resultado por criterio. */
export function ResumenEvaluacion({ evaluacion }: { evaluacion: EvaluacionDto }) {
  const columnas: ColumnaTabla<ResultadoCriterioDto>[] = [
    {
      clave: 'criterio',
      encabezado: 'Criterio',
      celda: (c) => (
        <span className="flex flex-wrap items-center gap-2 font-medium">
          {c.nombre}
          {c.falloCritico && <Badge tono="error" sinIcono>Falla crítica</Badge>}
        </span>
      ),
    },
    { clave: 'ia', encabezado: 'IA', alinear: 'derecha', celda: (c) => <span className="tabular-nums">{formatearNumero(c.puntajeIA)}</span>, ocultarEnMovil: true },
    { clave: 'vigente', encabezado: 'Vigente', alinear: 'derecha', celda: (c) => <span className="font-medium tabular-nums">{formatearNumero(c.puntajeVigente)}</span> },
    { clave: 'cumple', encabezado: 'Resultado', celda: (c) => <BadgeCumplimiento cumple={c.cumple} /> },
    { clave: 'evidencias', encabezado: 'Evidencias', alinear: 'derecha', celda: (c) => <span className="tabular-nums">{c.evidencias.length}</span>, ocultarEnMovil: true },
  ]

  return (
    <Tarjeta sinRelleno>
      <div className="flex flex-wrap items-end justify-between gap-4 border-b border-borde px-6 py-5">
        <div>
          <p className="text-meta text-texto-secundario">Puntaje total</p>
          <p className="font-titulo text-h1 font-bold tabular-nums">{formatearNumero(evaluacion.puntajeTotal)}</p>
        </div>
        <div className="flex flex-col items-end gap-2">
          <div className="flex flex-wrap justify-end gap-2">
            <BadgeOficial requiereRevision={!evaluacion.oficial} />
            <Badge tono="neutro" sinIcono>{ETIQUETA_ESTADO_EVALUACION[evaluacion.estado]}</Badge>
          </div>
          <p className="text-meta text-texto-secundario">Revisión {evaluacion.revision} · {evaluacion.modeloUsado}</p>
        </div>
      </div>
      <Tabla descripcion="Resultado por criterio" columnas={columnas} filas={evaluacion.criterios} claveFila={(c) => c.criterioId} />
    </Tarjeta>
  )
}
