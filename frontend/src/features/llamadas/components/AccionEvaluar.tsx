import { useState } from 'react'
import { extraerMensajeErrorBackend } from '../../../shared/api/extraerMensajeErrorBackend'
import { Boton } from '../../../shared/components/Boton'
import { AlertaFormulario } from '../../../shared/components/Formulario'
import { IconoRecargar } from '../../../shared/components/Iconos'
import { iniciarEvaluacion } from '../api/llamadasApi'
import type { LlamadaDto } from '../types/llamada.types'

interface PropsAccionEvaluar {
  llamada: LlamadaDto
  onIniciada: () => void
}

/** "Evaluar" (Disponible) o "Reintentar" (Fallida). Solo el Supervisor asignado puede iniciar la evaluación. */
export function AccionEvaluar({ llamada, onIniciada }: PropsAccionEvaluar) {
  const [enviando, setEnviando] = useState(false)
  const [error, setError] = useState<string | null>(null)

  if (llamada.estado !== 'Disponible' && llamada.estado !== 'Fallida') return null
  const reintento = llamada.estado === 'Fallida'

  const iniciar = async () => {
    setError(null)
    setEnviando(true)
    try {
      await iniciarEvaluacion(llamada.id)
      onIniciada()
    } catch (e) {
      setError(extraerMensajeErrorBackend(e))
    } finally {
      setEnviando(false)
    }
  }

  return (
    <div className="flex flex-col items-end gap-2">
      <Boton
        cargando={enviando}
        icono={reintento ? <IconoRecargar tamano={18} /> : undefined}
        onClick={iniciar}
      >
        {reintento ? 'Reintentar evaluación' : 'Evaluar con IA'}
      </Boton>
      <AlertaFormulario mensaje={error} />
    </div>
  )
}
