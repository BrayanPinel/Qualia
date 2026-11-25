import { useEffect, useState } from 'react'
import type { ReactNode } from 'react'
import { borrarSesion, guardarSesion, leerSesion, suscribirseASesion } from '../../../shared/api/almacenSesion'
import type { SesionActiva } from '../../../shared/types/comunes.types'
import { SesionContexto } from './sesionContexto'

/**
 * Guarda token, usuario, rol y empresaId (localStorage vía almacenSesion) y los comparte con la app.
 * Escucha el almacén para enterarse de un cierre forzado por el interceptor (401)
 * y programa el cierre automático cuando llega expiraEn.
 */
export function SesionProvider({ children }: { children: ReactNode }) {
  const [sesion, setSesion] = useState<SesionActiva | null>(leerSesion)

  useEffect(() => suscribirseASesion(setSesion), [])

  useEffect(() => {
    if (!sesion) return
    const restante = new Date(sesion.expiraEn).getTime() - Date.now()
    const temporizador = window.setTimeout(borrarSesion, Math.max(0, restante))
    return () => window.clearTimeout(temporizador)
  }, [sesion])

  return (
    <SesionContexto value={{ sesion, abrirSesion: guardarSesion, cerrarSesion: borrarSesion }}>
      {children}
    </SesionContexto>
  )
}
