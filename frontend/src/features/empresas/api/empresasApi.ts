import { apiClient } from '../../../shared/api/apiClient'
import type { EmpresaRegistradaDto, RegistrarEmpresaDto } from '../types/empresa.types'

/** POST /api/empresas: registro público (empresa + AdminEmpresa + suscripción de prueba). */
export async function registrarEmpresa(dto: RegistrarEmpresaDto): Promise<EmpresaRegistradaDto> {
  const { data } = await apiClient.post<EmpresaRegistradaDto>('/empresas', dto)
  return data
}
