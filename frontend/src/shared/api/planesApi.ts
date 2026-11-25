import { apiClient } from './apiClient'
import type { PlanDto } from '../types/suscripciones.types'

/** GET /api/planes: catálogo público de planes activos (registro, plataforma y suscripción). */
export async function obtenerPlanes(): Promise<PlanDto[]> {
  const { data } = await apiClient.get<PlanDto[]>('/planes')
  return data
}
