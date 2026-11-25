import type { EstadoEmpresa } from '../../../shared/types/comunes.types'
import type { ConsumoDto, EstadoSuscripcion, SuscripcionDto } from '../../../shared/types/suscripciones.types'

/** EmpresaComercialDto: vista comercial de una empresa, sin contenido de llamadas. */
export interface EmpresaComercialDto {
  empresaId: string
  codigo: string
  nombre: string
  estado: EstadoEmpresa
  suscripcion: SuscripcionDto | null
  consumo: ConsumoDto | null
}

/** CambiarSuscripcionDto. fechaVencimiento en UTC (ISO con Z). */
export interface CambiarSuscripcionDto {
  planId: string
  estado: EstadoSuscripcion
  fechaVencimiento: string
  permiteExcedente: boolean
  motivo: string
}
