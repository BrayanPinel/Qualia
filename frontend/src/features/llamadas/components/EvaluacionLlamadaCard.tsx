import { Link } from 'react-router-dom'
import { Badge } from '../../../shared/components/Badge'
import { BadgeOficial } from '../../../shared/components/estadosBadge'
import { EstadoCarga, EstadoError } from '../../../shared/components/EstadosVista'
import { Tarjeta } from '../../../shared/components/Tarjeta'
import { useConsulta } from '../../../shared/hooks/useConsulta'
import type { Rol } from '../../../shared/types/comunes.types'
import { formatearNumero } from '../../../shared/utils/formato'
import { obtenerEvaluacion } from '../api/llamadasApi'
import { ETIQUETA_ESTADO_EVALUACION } from '../types/llamada.types'
import { CLASE_ENLACE_ACCION } from '../utils/estilos'
import { CriterioEvaluacionCard } from './CriterioEvaluacionCard'

interface PropsEvaluacion {
  llamadaId: string
  rol: Rol
  claveEstado: string
  onSaltar: (segundos: number) => void
}

/** Evaluación de la llamada: puntaje total, carácter oficial y resultado por criterio con su evidencia. */
export function EvaluacionLlamadaCard({ llamadaId, rol, claveEstado, onSaltar }: PropsEvaluacion) {
  const { datos, error, cargando, recargar } = useConsulta(`evaluacion-${llamadaId}-${claveEstado}`, () =>
    obtenerEvaluacion(llamadaId),
  )

  const revisar = datos?.requiereRevision && rol === 'Supervisor' && (
    <Link to={`/revision/${llamadaId}`} className={`${CLASE_ENLACE_ACCION} border border-borde`}>
      Revisar evaluación
    </Link>
  )

  return (
    <Tarjeta titulo="Evaluación" acciones={revisar || undefined}>
      {error && <EstadoError mensaje={error} onReintentar={recargar} />}
      {!error && !datos && cargando && <EstadoCarga texto="Cargando evaluación…" />}
      {!error && datos && (
        <div className="flex flex-col gap-6">
          <div className="flex flex-wrap items-end justify-between gap-4 rounded-lg bg-alabastro px-5 py-4">
            <div>
              <p className="text-meta text-texto-secundario">Puntaje total</p>
              <p className="font-titulo text-h1 font-bold tabular-nums">{formatearNumero(datos.puntajeTotal)}</p>
            </div>
            <div className="flex flex-col items-end gap-2">
              <div className="flex flex-wrap justify-end gap-2">
                <BadgeOficial requiereRevision={!datos.oficial} />
                <Badge tono="neutro" sinIcono>
                  {ETIQUETA_ESTADO_EVALUACION[datos.estado]}
                </Badge>
              </div>
              <p className="text-meta text-texto-secundario">
                IA {datos.modoIA === 'Simulado' ? 'simulada' : 'real'} · {datos.modeloUsado} · Revisión {datos.revision}
              </p>
            </div>
          </div>
          <div className="flex flex-col gap-4">
            {datos.criterios.map((c) => (
              <CriterioEvaluacionCard key={c.criterioId} criterio={c} onSaltar={onSaltar} />
            ))}
          </div>
        </div>
      )}
    </Tarjeta>
  )
}
