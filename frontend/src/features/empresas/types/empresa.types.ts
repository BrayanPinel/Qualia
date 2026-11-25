import type { SuscripcionDto } from '../../../shared/types/suscripciones.types'

/** RegistrarEmpresaDto { codigoEmpresa, nombreEmpresa, planId, nombreAdministrador, email, password }. */
export interface RegistrarEmpresaDto {
  codigoEmpresa: string
  nombreEmpresa: string
  planId: string
  nombreAdministrador: string
  email: string
  password: string
}

/** Respuesta de POST /api/empresas. usuarioAdministradorId es el AdminEmpresa creado. */
export interface EmpresaRegistradaDto {
  empresaId: string
  codigoEmpresa: string
  usuarioAdministradorId: string
  suscripcion: SuscripcionDto
}
