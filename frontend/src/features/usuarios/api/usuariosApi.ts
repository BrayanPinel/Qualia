import { apiClient } from '../../../shared/api/apiClient'
import { rutaEmpresa } from '../../../shared/api/rutaEmpresa'
import type { PaginaDto, Paginacion, UsuarioDto } from '../../../shared/types/comunes.types'
import type { ActualizarUsuarioDto, CrearUsuarioDto } from '../types/usuario.types'

/** GET {e}/usuarios: paginado y ordenado por nombre (solo AdminEmpresa). */
export async function listarUsuarios(paginacion: Paginacion): Promise<PaginaDto<UsuarioDto>> {
  const { data } = await apiClient.get<PaginaDto<UsuarioDto>>(rutaEmpresa('/usuarios'), { params: paginacion })
  return data
}

/** POST {e}/usuarios → 201 UsuarioDto. */
export async function crearUsuario(dto: CrearUsuarioDto): Promise<UsuarioDto> {
  const { data } = await apiClient.post<UsuarioDto>(rutaEmpresa('/usuarios'), dto)
  return data
}

/** PUT {e}/usuarios/{id} → 204. No permite dejar a la empresa sin AdminEmpresa activo (409). */
export async function actualizarUsuario(id: string, dto: ActualizarUsuarioDto): Promise<void> {
  await apiClient.put(rutaEmpresa(`/usuarios/${id}`), dto)
}
