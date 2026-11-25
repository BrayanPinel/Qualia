import { use } from 'react'
import { SesionContexto } from './sesionContexto'

export function useSesion() {
  const valor = use(SesionContexto)
  if (!valor) {
    throw new Error('useSesion debe usarse dentro de SesionProvider.')
  }
  return valor
}
