import { useEffect, useEffectEvent, useState } from 'react'
import { extraerMensajeErrorBackend } from '../api/extraerMensajeErrorBackend'

interface Resultado<T> {
  clave: string
  datos?: T
  error?: string
}

/**
 * Ejecuta una consulta GET cuando cambia la clave y expone datos, error y cargando.
 * La clave debe incluir todo lo que cambia la consulta (página, filtros…).
 * Mientras recarga se conservan los datos anteriores para que la tabla no parpadee.
 * El error es el mensaje real del backend (extraerMensajeErrorBackend).
 */
export function useConsulta<T>(clave: string, consultar: () => Promise<T>) {
  const [version, setVersion] = useState(0)
  const [resultado, setResultado] = useState<Resultado<T> | null>(null)
  const [ultimosDatos, setUltimosDatos] = useState<T | undefined>(undefined)
  const claveActual = `${clave}#${version}`

  const ejecutar = useEffectEvent(() => consultar())

  useEffect(() => {
    let vigente = true
    ejecutar()
      .then((datos) => {
        if (!vigente) return
        setResultado({ clave: claveActual, datos })
        setUltimosDatos(datos)
      })
      .catch((error: unknown) => {
        if (vigente) setResultado({ clave: claveActual, error: extraerMensajeErrorBackend(error) })
      })
    return () => {
      vigente = false
    }
  }, [claveActual])

  const cargando = resultado?.clave !== claveActual
  return {
    datos: cargando ? ultimosDatos : resultado?.datos,
    error: cargando ? undefined : resultado?.error,
    cargando,
    recargar: () => setVersion((v) => v + 1),
  }
}
