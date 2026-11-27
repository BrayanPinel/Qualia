import { apiClient } from '../../../shared/api/apiClient'
import { rutaEmpresa } from '../../../shared/api/rutaEmpresa'
import type { ConsumoDto, SuscripcionDto } from '../../../shared/types/suscripciones.types'
import type { PagoSimuladoDto } from '../types/suscripcion.types'

/** GET {e}/suscripcion (solo AdminEmpresa): plan, estado, vigencia y periodo de consumo en curso. */
export async function obtenerSuscripcion(): Promise<SuscripcionDto> {
  const { data } = await apiClient.get<SuscripcionDto>(rutaEmpresa('/suscripcion'))
  return data
}

/** POST {e}/suscripcion/pago-simulado → 200 SuscripcionDto. Un pago rechazado responde con el error del backend. */
export async function registrarPagoSimulado(dto: PagoSimuladoDto): Promise<SuscripcionDto> {
  const { data } = await apiClient.post<SuscripcionDto>(rutaEmpresa('/suscripcion/pago-simulado'), dto)
  return data
}

/** GET {e}/consumo?periodo=YYYY-MM (sin periodo, el mes UTC en curso). */
export async function obtenerConsumo(periodo: string): Promise<ConsumoDto> {
  const { data } = await apiClient.get<ConsumoDto>(rutaEmpresa('/consumo'), { params: { periodo } })
  return data
}
