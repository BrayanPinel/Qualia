import { createContext } from 'react'
import type { SesionActiva } from '../../../shared/types/comunes.types'

export interface ValorSesion {
  sesion: SesionActiva | null
  abrirSesion: (sesion: SesionActiva) => void
  cerrarSesion: () => void
}

export const SesionContexto = createContext<ValorSesion | null>(null)
