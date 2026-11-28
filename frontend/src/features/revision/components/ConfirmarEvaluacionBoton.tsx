import { useState } from 'react'
import { extraerMensajeErrorBackend } from '../../../shared/api/extraerMensajeErrorBackend'
import { Boton } from '../../../shared/components/Boton'
import { Dialogo } from '../../../shared/components/Dialogo'
import { AlertaFormulario } from '../../../shared/components/Formulario'
import { IconoCheck } from '../../../shared/components/Iconos'
import { formatearNumero } from '../../../shared/utils/formato'
import type { EvaluacionDto } from '../../llamadas/types/llamada.types'
import { confirmarEvaluacion } from '../api/revisionApi'
import { esConflictoDeRevision } from '../utils/conflicto'

interface PropsConfirmar {
  evaluacion: EvaluacionDto
  onConfirmada: () => void
  onConflicto: (mensaje: string) => void
}

/** Confirma la propuesta de la IA tal cual: la evaluación queda oficial. Una evidencia inválida exige ajustar. */
export function ConfirmarEvaluacionBoton({ evaluacion, onConfirmada, onConflicto }: PropsConfirmar) {
  const [abierto, setAbierto] = useState(false)
  const [enviando, setEnviando] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const cerrar = () => {
    setAbierto(false)
    setError(null)
  }

  const confirmar = async () => {
    setEnviando(true)
    setError(null)
    try {
      await confirmarEvaluacion(evaluacion.id, evaluacion.revision)
      setAbierto(false)
      onConfirmada()
    } catch (e) {
      // EVIDENCIA_INVALIDA se resuelve ajustando, no recargando: se muestra en el diálogo.
      if (esConflictoDeRevision(e)) {
        setAbierto(false)
        onConflicto(extraerMensajeErrorBackend(e))
      } else {
        setError(extraerMensajeErrorBackend(e))
      }
    } finally {
      setEnviando(false)
    }
  }

  return (
    <>
      <Boton icono={<IconoCheck tamano={18} />} onClick={() => setAbierto(true)}>
        Confirmar evaluación de la IA
      </Boton>
      <Dialogo
        abierto={abierto}
        titulo="Confirmar evaluación"
        descripcion={`Los puntajes actuales (total ${formatearNumero(evaluacion.puntajeTotal)}) quedan como oficiales y la llamada sale de la bandeja de revisión.`}
        onCerrar={cerrar}
      >
        <div className="flex flex-col gap-5">
          <AlertaFormulario mensaje={error} />
          <div className="flex justify-end gap-2">
            <Boton variante="secundario" onClick={cerrar}>
              Cancelar
            </Boton>
            <Boton icono={<IconoCheck tamano={18} />} cargando={enviando} onClick={confirmar}>
              Confirmar evaluación
            </Boton>
          </div>
        </div>
      </Dialogo>
    </>
  )
}
