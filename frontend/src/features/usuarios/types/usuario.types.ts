import type { RolEmpresa } from '../../../shared/types/comunes.types'

// La salida es UsuarioDto (shared/types): un solo DTO de usuario en toda la API.

/** CrearUsuarioDto { nombre, email, password, rol }. La empresa sale de la sesión. */
export interface CrearUsuarioDto {
  nombre: string
  email: string
  password: string
  rol: RolEmpresa
}

/** ActualizarUsuarioDto { nombre, activo }. */
export interface ActualizarUsuarioDto {
  nombre: string
  activo: boolean
}
