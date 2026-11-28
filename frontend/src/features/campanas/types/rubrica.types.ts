import type { EstadoRubrica } from '../../../shared/types/comunes.types'

// Rúbricas versionadas: espejo de CampanasDtos.cs. Borrador → Activa (publicada) → Retirada.

export interface CriterioDto {
  id: string
  nombre: string
  descripcion: string
  peso: number
  esCritico: boolean
  umbralCumplimiento: number
}

export interface RubricaDto {
  id: string
  empresaId: string
  campanaId: string
  nombre: string
  version: number
  estado: EstadoRubrica
  criterios: CriterioDto[]
}

/** CriterioEntradaDto: igual que CriterioDto pero sin id (el backend reemplaza todos los criterios). */
export interface CriterioEntradaDto {
  nombre: string
  descripcion: string
  peso: number
  esCritico: boolean
  umbralCumplimiento: number
}

/** CrearRubricaDto { nombre, criterios[] }: POST crea una versión nueva en Borrador; PUT reemplaza un Borrador. */
export interface CrearRubricaDto {
  nombre: string
  criterios: CriterioEntradaDto[]
}

/** Rubrica.MinimoCriterios, MaximoCriterios y SumaPesosRequerida. */
export const MINIMO_CRITERIOS = 1
export const MAXIMO_CRITERIOS = 30
export const SUMA_PESOS_REQUERIDA = 100

/** Segmento de la ruta del editor para una versión que aún no existe. */
export const RUBRICA_NUEVA = 'nueva'

/** Parámetro de la URL del editor nuevo con la versión que se copia (?base=<id>). */
export const PARAMETRO_BASE = 'base'

/** Ruta del editor de una versión nueva, opcionalmente copiando otra versión de la campaña. */
export function rutaNuevaVersion(campanaId: string, baseId?: string): string {
  const ruta = `/campanas/${campanaId}/rubricas/${RUBRICA_NUEVA}`
  return baseId ? `${ruta}?${PARAMETRO_BASE}=${baseId}` : ruta
}
