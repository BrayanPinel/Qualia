import { Badge } from '../../../shared/components/Badge'
import { EstadoCarga, EstadoError, EstadoVacio } from '../../../shared/components/EstadosVista'
import { Tarjeta } from '../../../shared/components/Tarjeta'
import { useConsulta } from '../../../shared/hooks/useConsulta'
import { obtenerTranscripcion } from '../api/llamadasApi'
import { formatearSegundos } from '../utils/tiempo'

interface PropsTranscripcion {
  llamadaId: string
  /** Cambia cuando cambia el estado de la llamada: la transcripción aparece al terminar de transcribir. */
  claveEstado: string
  onSaltar: (segundos: number) => void
}

/** Transcripción por segmentos (texto ya enmascarado por el backend). */
export function TranscripcionCard({ llamadaId, claveEstado, onSaltar }: PropsTranscripcion) {
  const { datos, error, cargando, recargar } = useConsulta(`transcripcion-${llamadaId}-${claveEstado}`, () =>
    obtenerTranscripcion(llamadaId),
  )

  return (
    <Tarjeta titulo="Transcripción" descripcion="Los datos sensibles llegan enmascarados.">
      {error && <EstadoError mensaje={error} onReintentar={recargar} />}
      {!error && !datos && cargando && <EstadoCarga texto="Cargando transcripción…" />}
      {!error && datos && datos.length === 0 && (
        <EstadoVacio titulo="Sin transcripción" descripcion="La transcripción aparece cuando la llamada termina de procesarse." />
      )}
      {!error && datos && datos.length > 0 && (
        <ol className="flex max-h-[32rem] flex-col gap-3 overflow-y-auto pr-1">
          {datos.map((s) => (
            <li key={s.id} className={`flex flex-col gap-1 rounded-lg px-3 py-2 ${s.hablante === 'Agente' ? 'bg-alabastro' : ''}`}>
              <div className="flex items-center gap-2">
                <Badge tono="neutro" sinIcono>
                  {s.hablante}
                </Badge>
                <button
                  type="button"
                  onClick={() => onSaltar(s.inicioSeg)}
                  className="text-meta text-texto-secundario tabular-nums underline-offset-2 hover:text-carbon hover:underline"
                  aria-label={`Reproducir desde ${formatearSegundos(s.inicioSeg)}`}
                >
                  {formatearSegundos(s.inicioSeg)} – {formatearSegundos(s.finSeg)}
                </button>
              </div>
              <p className="text-sm">{s.textoRedactado}</p>
            </li>
          ))}
        </ol>
      )}
    </Tarjeta>
  )
}
