import { useRef } from 'react'
import { Link, useParams } from 'react-router-dom'
import { EncabezadoPagina } from '../../../shared/components/EncabezadoPagina'
import { EstadoCarga, EstadoError } from '../../../shared/components/EstadosVista'
import { IconoFlechaIzq } from '../../../shared/components/Iconos'
import { useSesion } from '../../auth/context/useSesion'
import { useCatalogosLlamadas } from '../hooks/useCatalogosLlamadas'
import { useSeguimientoLlamada } from '../hooks/useSeguimientoLlamada'
import { CLASE_ENLACE_ACCION } from '../utils/estilos'
import { idCorto } from '../utils/tiempo'
import { AccionEvaluar } from './AccionEvaluar'
import { EvaluacionLlamadaCard } from './EvaluacionLlamadaCard'
import { ReproductorAudio, type ControlAudio } from './ReproductorAudio'
import { ResumenLlamadaCard } from './ResumenLlamadaCard'
import { TranscripcionCard } from './TranscripcionCard'

/** Detalle de una llamada: estado en vivo, audio, evaluación con evidencias y transcripción. */
export function DetalleLlamadaView() {
  const { id = '' } = useParams()
  const { sesion } = useSesion()
  const rol = sesion?.rol ?? 'Agente'
  const catalogos = useCatalogosLlamadas(rol)
  const { datos: llamada, error, recargar, enProceso } = useSeguimientoLlamada(id)
  const audio = useRef<ControlAudio>(null)

  const saltarA = (segundos: number) => audio.current?.saltarA(segundos)
  const volver = (
    <Link to="/llamadas" className={CLASE_ENLACE_ACCION}>
      <IconoFlechaIzq tamano={18} />
      Volver a llamadas
    </Link>
  )

  if (error) {
    return (
      <>
        <EncabezadoPagina titulo="Detalle de llamada" acciones={volver} />
        <EstadoError mensaje={error} onReintentar={recargar} />
      </>
    )
  }
  if (!llamada) return <EstadoCarga texto="Cargando llamada…" />

  const tieneEvaluacion = llamada.requiereRevision !== null
  const acciones = (
    <>
      {volver}
      {rol === 'Supervisor' && <AccionEvaluar llamada={llamada} onIniciada={recargar} />}
    </>
  )

  return (
    <>
      <EncabezadoPagina titulo={`Llamada ${idCorto(llamada.id)}`} acciones={acciones} />
      <div className="flex flex-col gap-6">
        <ResumenLlamadaCard
          llamada={llamada}
          nombreCampana={catalogos.nombreCampana(llamada.campanaId)}
          nombreAgente={catalogos.nombreAgente?.(llamada.agenteId) ?? null}
          enProceso={enProceso}
        />
        <ReproductorAudio llamadaId={llamada.id} control={audio} />
        <div className="grid gap-6 xl:grid-cols-[3fr_2fr]">
          {tieneEvaluacion ? (
            <EvaluacionLlamadaCard llamadaId={llamada.id} rol={rol} claveEstado={llamada.estado} onSaltar={saltarA} />
          ) : (
            <p className="rounded-lg border border-dashed border-borde px-6 py-10 text-center text-sm text-texto-secundario">
              La evaluación aparece cuando la IA termina de procesar la llamada.
            </p>
          )}
          <TranscripcionCard llamadaId={llamada.id} claveEstado={llamada.estado} onSaltar={saltarA} />
        </div>
      </div>
    </>
  )
}
