import { Link, useParams } from 'react-router-dom'
import { EncabezadoPagina } from '../../../shared/components/EncabezadoPagina'
import { EstadoCarga, EstadoError } from '../../../shared/components/EstadosVista'
import { BadgeEstadoDisputa } from '../../../shared/components/estadosBadge'
import { IconoFlechaIzq } from '../../../shared/components/Iconos'
import { Tarjeta } from '../../../shared/components/Tarjeta'
import { useConsulta } from '../../../shared/hooks/useConsulta'
import { formatearFecha } from '../../../shared/utils/formato'
import { useSesion } from '../../auth/context/useSesion'
import { CLASE_ENLACE_ACCION } from '../../llamadas/utils/estilos'
import { idCorto } from '../../llamadas/utils/tiempo'
import { obtenerDisputa } from '../api/disputasApi'
import { ResolverDisputaPanel } from './ResolverDisputaPanel'

/** /disputas/:id — motivo, estado y resolución. El Supervisor asignado la resuelve mientras está abierta. */
export function DetalleDisputaView() {
  const { id = '' } = useParams()
  const { sesion } = useSesion()
  const { datos: disputa, error, recargar } = useConsulta(`disputa-${id}`, () => obtenerDisputa(id))

  const volver = (
    <Link to="/disputas" className={CLASE_ENLACE_ACCION}>
      <IconoFlechaIzq tamano={18} />
      Volver a disputas
    </Link>
  )

  if (error) {
    return (
      <>
        <EncabezadoPagina titulo="Disputa" acciones={volver} />
        <EstadoError mensaje={error} onReintentar={recargar} />
      </>
    )
  }
  if (!disputa) return <EstadoCarga texto="Cargando disputa…" />

  const puedeResolver = sesion?.rol === 'Supervisor' && disputa.estado === 'Abierta'

  return (
    <>
      <EncabezadoPagina
        titulo={`Disputa de la llamada ${idCorto(disputa.llamadaId)}`}
        acciones={
          <>
            {volver}
            <Link to={`/llamadas/${disputa.llamadaId}`} className={CLASE_ENLACE_ACCION}>Ver llamada y audio</Link>
          </>
        }
      />
      <div className="flex flex-col gap-6">
        <Tarjeta>
          <div className="flex flex-col gap-4">
            <div className="flex flex-wrap items-center gap-3 text-meta text-texto-secundario">
              <BadgeEstadoDisputa estado={disputa.estado} />
              <span>Abierta el {formatearFecha(disputa.creadaEn)}</span>
              {disputa.resueltaEn && <span>· Resuelta el {formatearFecha(disputa.resueltaEn)}</span>}
            </div>
            <div>
              <p className="text-sm font-medium">Motivo del agente</p>
              <p className="mt-1 whitespace-pre-line text-texto-secundario">{disputa.motivo}</p>
            </div>
            {disputa.resolucion && (
              <div className="rounded-lg bg-alabastro px-4 py-3">
                <p className="text-sm font-medium">Resolución del supervisor</p>
                <p className="mt-1 whitespace-pre-line text-texto-secundario">{disputa.resolucion}</p>
              </div>
            )}
          </div>
        </Tarjeta>

        {puedeResolver && <ResolverDisputaPanel disputaId={disputa.id} llamadaId={disputa.llamadaId} onResuelta={recargar} />}
      </div>
    </>
  )
}
