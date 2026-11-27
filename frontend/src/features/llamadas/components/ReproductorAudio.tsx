import { useEffect, useEffectEvent, useImperativeHandle, useRef, useState } from 'react'
import type { Ref } from 'react'
import { EstadoCarga, EstadoError } from '../../../shared/components/EstadosVista'
import { Tarjeta } from '../../../shared/components/Tarjeta'
import { useConsulta } from '../../../shared/hooks/useConsulta'
import { obtenerAccesoAudio } from '../api/llamadasApi'

/** Control que usan la evaluación y la transcripción para saltar al instante de una evidencia. */
export interface ControlAudio {
  saltarA: (segundos: number) => void
}

interface PropsReproductor {
  llamadaId: string
  control: Ref<ControlAudio>
}

/**
 * Reproductor con la URL firmada del backend (vence a los 10 min). Al vencer se pide otra:
 * en pausa se renueva al instante; si falla la carga (token vencido) se renueva y se retoma en la misma posición.
 */
export function ReproductorAudio({ llamadaId, control }: PropsReproductor) {
  const audioRef = useRef<HTMLAudioElement>(null)
  const pendiente = useRef<{ segundos: number; reproducir: boolean } | null>(null)
  const [version, setVersion] = useState(0)
  const acceso = useConsulta(`audio-${llamadaId}-${version}`, () => obtenerAccesoAudio(llamadaId))

  const renovar = (reproducir: boolean) => {
    const audio = audioRef.current
    pendiente.current = { segundos: audio?.currentTime ?? 0, reproducir }
    setVersion((v) => v + 1)
  }

  const alVencer = useEffectEvent(() => {
    if (audioRef.current?.paused !== false) renovar(false)
  })

  useEffect(() => {
    if (!acceso.datos) return
    const restante = new Date(acceso.datos.expiraEn).getTime() - Date.now()
    const temporizador = setTimeout(() => alVencer(), Math.max(restante, 0))
    return () => clearTimeout(temporizador)
  }, [acceso.datos])

  useImperativeHandle(control, () => ({
    saltarA: (segundos: number) => {
      const audio = audioRef.current
      if (!audio) return
      audio.scrollIntoView({ behavior: 'smooth', block: 'nearest' })
      if (audio.readyState === 0) {
        pendiente.current = { segundos, reproducir: true }
        audio.load()
        return
      }
      audio.currentTime = segundos
      void audio.play().catch(() => undefined)
    },
  }))

  const alCargarMetadatos = () => {
    const audio = audioRef.current
    const salto = pendiente.current
    if (!audio || !salto) return
    pendiente.current = null
    audio.currentTime = salto.segundos
    if (salto.reproducir) void audio.play().catch(() => undefined)
  }

  const alFallar = () => {
    // Solo se renueva si la URL ya venció: así un audio dañado no provoca un ciclo de peticiones.
    if (acceso.datos && new Date(acceso.datos.expiraEn).getTime() <= Date.now()) renovar(true)
  }

  return (
    <Tarjeta titulo="Audio" descripcion="Al elegir una evidencia, el audio salta al instante exacto del fragmento.">
      {acceso.error && <EstadoError mensaje={acceso.error} onReintentar={acceso.recargar} />}
      {!acceso.error && !acceso.datos && <EstadoCarga texto="Preparando el audio…" />}
      {!acceso.error && acceso.datos && (
        <audio
          ref={audioRef}
          src={acceso.datos.url}
          controls
          preload="metadata"
          onLoadedMetadata={alCargarMetadatos}
          onError={alFallar}
          className="w-full"
        >
          Tu navegador no puede reproducir este audio.
        </audio>
      )}
    </Tarjeta>
  )
}
