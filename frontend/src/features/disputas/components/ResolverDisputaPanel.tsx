import { useState } from 'react'
import { EstadoCarga, EstadoError } from '../../../shared/components/EstadosVista'
import { Tarjeta } from '../../../shared/components/Tarjeta'
import { useConsulta } from '../../../shared/hooks/useConsulta'
import { cargarDatosRevision } from '../../revision/api/revisionApi'
import { ConflictoRevision } from '../../revision/components/ConflictoRevision'
import { ResumenEvaluacion } from '../../revision/components/ResumenEvaluacion'
import { ResolverDisputaForm } from './ResolverDisputaForm'

interface PropsPanel {
  disputaId: string
  llamadaId: string
  onResuelta: () => void
}

/** Supervisor: evaluación vigente de la llamada disputada y formulario de resolución. */
export function ResolverDisputaPanel({ disputaId, llamadaId, onResuelta }: PropsPanel) {
  const [conflicto, setConflicto] = useState<string | null>(null)
  const { datos, error, recargar } = useConsulta(`resolver-${llamadaId}`, () => cargarDatosRevision(llamadaId))

  // DISPUTA_YA_RESUELTA o REVISION_DESACTUALIZADA: se recarga la disputa (por si ya se resolvió) y la evaluación.
  const recargarTodo = () => {
    setConflicto(null)
    recargar()
    onResuelta()
  }

  if (error) return <EstadoError mensaje={error} onReintentar={recargar} />
  if (!datos) return <EstadoCarga texto="Cargando evaluación…" />

  return (
    <div className="flex flex-col gap-6">
      <ResumenEvaluacion evaluacion={datos.evaluacion} />
      {conflicto ? (
        <ConflictoRevision mensaje={conflicto} onRecargar={recargarTodo} />
      ) : (
        <Tarjeta titulo="Resolver disputa" descripcion={`Revisión esperada de la evaluación: ${datos.evaluacion.revision}.`}>
          <ResolverDisputaForm
            key={datos.evaluacion.revision}
            disputaId={disputaId}
            evaluacion={datos.evaluacion}
            segmentos={datos.segmentos}
            onResuelta={onResuelta}
            onConflicto={setConflicto}
          />
        </Tarjeta>
      )}
    </div>
  )
}
