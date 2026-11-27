import { Badge } from '../../../shared/components/Badge'
import { BadgeCumplimiento } from '../../../shared/components/estadosBadge'
import { formatearNumero } from '../../../shared/utils/formato'
import type { ResultadoCriterioDto } from '../types/llamada.types'
import { formatearSegundos } from '../utils/tiempo'

interface PropsCriterio {
  criterio: ResultadoCriterioDto
  onSaltar: (segundos: number) => void
}

function Puntaje({ etiqueta, valor, destacado = false }: { etiqueta: string; valor: number | null; destacado?: boolean }) {
  return (
    <div className="flex flex-col">
      <dt className="text-meta text-texto-secundario">{etiqueta}</dt>
      <dd className={`tabular-nums ${destacado ? 'text-h3 font-semibold' : 'text-sm'}`}>
        {valor === null ? '—' : formatearNumero(valor)}
      </dd>
    </div>
  )
}

/** Resultado de un criterio: puntajes IA / humano / vigente, cumplimiento, justificación y evidencias. */
export function CriterioEvaluacionCard({ criterio, onSaltar }: PropsCriterio) {
  return (
    <article className="flex flex-col gap-4 rounded-lg border border-borde p-5">
      <header className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h3 className="font-medium">{criterio.nombre}</h3>
          <p className="text-meta text-texto-secundario">
            Peso {formatearNumero(criterio.peso)} % · Umbral {formatearNumero(criterio.umbralCumplimiento)}
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          <BadgeCumplimiento cumple={criterio.cumple} />
          {criterio.falloCritico && <Badge tono="error">Fallo crítico</Badge>}
        </div>
      </header>

      <dl className="grid grid-cols-3 gap-4">
        <Puntaje etiqueta="Vigente" valor={criterio.puntajeVigente} destacado />
        <Puntaje etiqueta="IA" valor={criterio.puntajeIA} />
        <Puntaje etiqueta="Supervisor" valor={criterio.puntajeHumano} />
      </dl>

      <p className="text-sm">{criterio.justificacion}</p>

      {criterio.evidencias.length > 0 && (
        <div className="flex flex-col gap-2">
          <p className="text-meta font-medium text-texto-secundario">Evidencias</p>
          <ul className="flex flex-col gap-2">
            {criterio.evidencias.map((e) => (
              <li key={`${e.segmentoId}-${e.inicioSeg}`}>
                <button
                  type="button"
                  onClick={() => onSaltar(e.inicioSeg)}
                  className="flex w-full flex-col gap-1 rounded-lg border border-borde bg-alabastro px-3 py-2 text-left transition-colors hover:border-texto-secundario"
                  aria-label={`Escuchar evidencia en ${formatearSegundos(e.inicioSeg)}`}
                >
                  <span className="text-meta text-texto-secundario tabular-nums">
                    {e.hablante} · {formatearSegundos(e.inicioSeg)} – {formatearSegundos(e.finSeg)} · Escuchar
                  </span>
                  <span className="text-sm">“{e.citaTextual}”</span>
                </button>
              </li>
            ))}
          </ul>
        </div>
      )}
    </article>
  )
}
