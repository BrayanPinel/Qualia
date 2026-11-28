import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { Boton } from '../../../shared/components/Boton'
import { EncabezadoPagina } from '../../../shared/components/EncabezadoPagina'
import { EstadoCarga, EstadoError } from '../../../shared/components/EstadosVista'
import { IconoEditar, IconoFlechaIzq } from '../../../shared/components/Iconos'
import { Tarjeta } from '../../../shared/components/Tarjeta'
import { useConsulta } from '../../../shared/hooks/useConsulta'
import { CLASE_ENLACE_ACCION } from '../../llamadas/utils/estilos'
import { idCorto } from '../../llamadas/utils/tiempo'
import { cargarDatosRevision } from '../api/revisionApi'
import { AjusteEvaluacionForm } from './AjusteEvaluacionForm'
import { ConfirmarEvaluacionBoton } from './ConfirmarEvaluacionBoton'
import { ConflictoRevision } from './ConflictoRevision'
import { ResumenEvaluacion } from './ResumenEvaluacion'

/**
 * /revision/:llamadaId — el Supervisor asignado confirma la propuesta de la IA o la ajusta.
 * Se abre desde la bandeja de revisión o el detalle de la llamada (por id de llamada, no de evaluación).
 */
export function RevisionEvaluacionView() {
  const { llamadaId = '' } = useParams()
  const navegar = useNavigate()
  const [ajustando, setAjustando] = useState(false)
  const [conflicto, setConflicto] = useState<string | null>(null)
  const { datos, error, recargar } = useConsulta(`revision-${llamadaId}`, () => cargarDatosRevision(llamadaId))

  const volver = (
    <Link to="/revision" className={CLASE_ENLACE_ACCION}>
      <IconoFlechaIzq tamano={18} />
      Volver a la bandeja
    </Link>
  )
  // El router no reinicia el scroll: tras decidir, el detalle de la llamada se muestra desde arriba.
  const terminar = () => {
    navegar(`/llamadas/${llamadaId}`)
    window.scrollTo(0, 0)
  }
  const alConflicto = (mensaje: string) => {
    setConflicto(mensaje)
    setAjustando(false)
  }
  const recargarEvaluacion = () => {
    setConflicto(null)
    recargar()
  }

  if (error) {
    return (
      <>
        <EncabezadoPagina titulo="Revisión de calidad" acciones={volver} />
        <EstadoError mensaje={error} onReintentar={recargar} />
      </>
    )
  }
  if (!datos) return <EstadoCarga texto="Cargando evaluación…" />

  const { evaluacion, segmentos } = datos
  const yaConfirmada = evaluacion.estado === 'ConfirmadaPorSupervisor'

  return (
    <>
      <EncabezadoPagina
        titulo={`Revisión de la llamada ${idCorto(llamadaId)}`}
        descripcion="Confirma la propuesta de la IA si es correcta o ajústala con motivo, justificación y evidencias."
        acciones={
          <>
            {volver}
            <Link to={`/llamadas/${llamadaId}`} className={CLASE_ENLACE_ACCION}>Ver llamada y audio</Link>
          </>
        }
      />
      <div className="flex flex-col gap-6">
        {conflicto && <ConflictoRevision mensaje={conflicto} onRecargar={recargarEvaluacion} />}
        <ResumenEvaluacion evaluacion={evaluacion} />

        {!ajustando && !conflicto && (
          <div className="flex flex-wrap justify-end gap-2">
            <Boton variante="secundario" icono={<IconoEditar tamano={18} />} onClick={() => setAjustando(true)}>
              Ajustar evaluación
            </Boton>
            {!yaConfirmada && <ConfirmarEvaluacionBoton evaluacion={evaluacion} onConfirmada={terminar} onConflicto={alConflicto} />}
          </div>
        )}

        {ajustando && (
          <Tarjeta titulo="Ajuste del supervisor" descripcion={`Incluye los ${evaluacion.criterios.length} criterios de la rúbrica. Revisión esperada: ${evaluacion.revision}.`}>
            <AjusteEvaluacionForm
              key={evaluacion.revision}
              evaluacion={evaluacion}
              segmentos={segmentos}
              onAjustada={terminar}
              onCancelar={() => setAjustando(false)}
              onConflicto={alConflicto}
            />
          </Tarjeta>
        )}
      </div>
    </>
  )
}
