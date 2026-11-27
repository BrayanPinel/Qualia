import { useEffect, useEffectEvent } from 'react'
import { useConsulta } from '../../../shared/hooks/useConsulta'
import { obtenerLlamada } from '../api/llamadasApi'
import { ESTADOS_EN_PROCESO } from '../types/llamada.types'

/** Cada cuánto se consulta el estado mientras el worker procesa la llamada (entre 3 y 5 s). */
export const INTERVALO_SEGUIMIENTO_MS = 4000

/**
 * Detalle de la llamada. Mientras esté en cola, transcribiendo o evaluando, vuelve a pedir el estado
 * con setInterval dentro de useEffect y lo limpia al terminar el proceso o al desmontar (Clase 13).
 */
export function useSeguimientoLlamada(id: string) {
  const consulta = useConsulta(`llamada-${id}`, () => obtenerLlamada(id))
  const enProceso = consulta.datos ? ESTADOS_EN_PROCESO.includes(consulta.datos.estado) : false

  const refrescar = useEffectEvent(() => consulta.recargar())

  useEffect(() => {
    if (!enProceso) return
    const temporizador = setInterval(() => refrescar(), INTERVALO_SEGUIMIENTO_MS)
    return () => clearInterval(temporizador)
  }, [enProceso])

  return { ...consulta, enProceso }
}
