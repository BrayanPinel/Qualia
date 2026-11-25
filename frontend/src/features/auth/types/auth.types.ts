import type { UsuarioDto } from '../../../shared/types/comunes.types'

/** LoginDto { codigoEmpresa, email, password }. */
export interface LoginDto {
  codigoEmpresa: string
  email: string
  password: string
}

/** LoginPlataformaDto { email, password }. */
export interface LoginPlataformaDto {
  email: string
  password: string
}

/** SesionDto { accessToken, tokenType, expiraEn, usuario, empresaId }. */
export interface SesionDto {
  accessToken: string
  tokenType: 'Bearer'
  expiraEn: string
  usuario: UsuarioDto
  empresaId: string
}

/** SesionPlataformaDto { accessToken, tokenType, expiraEn, administradorId, rol }. Sin empresa. */
export interface SesionPlataformaDto {
  accessToken: string
  tokenType: 'Bearer'
  expiraEn: string
  administradorId: string
  rol: 'PlatformAdmin'
}
