// CampanaDto { id, empresaId, nombre, activa, rubricaActivaId }. Lo usan varios módulos como selector de campaña.
export interface CampanaDto {
  id: string
  empresaId: string
  nombre: string
  activa: boolean
  rubricaActivaId: string | null
}
