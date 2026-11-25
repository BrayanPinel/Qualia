import { apiClient } from '../../../shared/api/apiClient'
import type { SesionActiva } from '../../../shared/types/comunes.types'
import type { LoginDto, LoginPlataformaDto, SesionDto, SesionPlataformaDto } from '../types/auth.types'

/** POST /api/auth/login. Devuelve la sesión lista para guardar. */
export async function iniciarSesion(dto: LoginDto): Promise<SesionActiva> {
  const { data } = await apiClient.post<SesionDto>('/auth/login', dto)
  return {
    accessToken: data.accessToken,
    expiraEn: data.expiraEn,
    rol: data.usuario.rol,
    empresaId: data.empresaId,
    codigoEmpresa: dto.codigoEmpresa.trim().toLowerCase(),
    usuario: { id: data.usuario.id, nombre: data.usuario.nombre, email: data.usuario.email },
  }
}

/** POST /api/plataforma/auth/login. PlatformAdmin no pertenece a ninguna empresa. */
export async function iniciarSesionPlataforma(dto: LoginPlataformaDto): Promise<SesionActiva> {
  const { data } = await apiClient.post<SesionPlataformaDto>('/plataforma/auth/login', dto)
  return {
    accessToken: data.accessToken,
    expiraEn: data.expiraEn,
    rol: data.rol,
    empresaId: null,
    codigoEmpresa: null,
    usuario: { id: data.administradorId, nombre: dto.email.trim(), email: dto.email.trim() },
  }
}
