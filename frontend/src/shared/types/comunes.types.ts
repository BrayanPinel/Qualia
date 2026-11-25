// Contratos comunes del backend. Espejo exacto de los DTOs de C# en camelCase.
// Los enums llegan como texto (JsonStringEnumConverter), por eso son uniones de texto.

/** ErrorDto { status, codigo, mensaje, errores?, traceId }: único formato de error de la API. */
export interface ErrorDto {
  status: number
  codigo: string
  mensaje: string
  errores?: Record<string, string[]> | null
  traceId: string
}

/** PaginaDto<T> { items, pagina, tamanoPagina, total }. */
export interface PaginaDto<T> {
  items: T[]
  pagina: number
  tamanoPagina: number
  total: number
}

/** Parámetros de paginación: pagina desde 1, tamanoPagina de 1 a 100. */
export interface Paginacion {
  pagina: number
  tamanoPagina: number
}

export type RolEmpresa = 'AdminEmpresa' | 'Supervisor' | 'Agente' | 'Auditor'
export type RolPlataforma = 'PlatformAdmin'
export type Rol = RolEmpresa | RolPlataforma

export const ROLES_EMPRESA: readonly RolEmpresa[] = ['AdminEmpresa', 'Supervisor', 'Agente', 'Auditor']

export const ETIQUETA_ROL: Record<Rol, string> = {
  AdminEmpresa: 'Administrador',
  Supervisor: 'Supervisor',
  Agente: 'Agente',
  Auditor: 'Auditor',
  PlatformAdmin: 'Administrador de plataforma',
}

export type EstadoEmpresa = 'Activa' | 'Suspendida'
export type EstadoRubrica = 'Borrador' | 'Activa' | 'Retirada'
export type EstadoLlamada =
  | 'Disponible'
  | 'EnCola'
  | 'Transcribiendo'
  | 'Transcrita'
  | 'Evaluando'
  | 'EvaluadaPorIA'
  | 'Fallida'
export type EstadoEvaluacion = 'EvaluadaPorIA' | 'ConfirmadaPorSupervisor' | 'AjustadaPorSupervisor'
export type EstadoDisputa = 'Abierta' | 'Resuelta'

/** UsuarioDto { id, empresaId, nombre, email, rol, activo }: un solo DTO de usuario en toda la API. */
export interface UsuarioDto {
  id: string
  empresaId: string
  nombre: string
  email: string
  rol: RolEmpresa
  activo: boolean
}

/**
 * Sesión guardada en el navegador. Se arma con SesionDto o SesionPlataformaDto.
 * codigoEmpresa y el email de plataforma son los que escribió el usuario al iniciar sesión,
 * porque el backend no los devuelve en la respuesta del login.
 */
export interface SesionActiva {
  accessToken: string
  expiraEn: string
  rol: Rol
  empresaId: string | null
  codigoEmpresa: string | null
  usuario: { id: string; nombre: string; email: string }
}
