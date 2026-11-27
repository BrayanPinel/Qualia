import { apiClient } from '../../../shared/api/apiClient'
import { rutaEmpresa } from '../../../shared/api/rutaEmpresa'
import type { PaginaDto, Paginacion, UsuarioDto } from '../../../shared/types/comunes.types'
import { ROLES_ASIGNABLES, type CampanaDto, type GuardarCampanaDto } from '../types/campana.types'

// Endpoints de campañas y asignaciones (AdminEmpresa).

/** Máximo que acepta PaginacionDtoValidator. */
const TAMANO_MAXIMO = 100

/** Recorre todas las páginas de un listado (asignados, candidatos, búsqueda de una campaña). */
async function leerTodo<T>(url: string): Promise<T[]> {
  const items: T[] = []
  for (let pagina = 1; ; pagina++) {
    const { data } = await apiClient.get<PaginaDto<T>>(url, { params: { pagina, tamanoPagina: TAMANO_MAXIMO } })
    items.push(...data.items)
    if (data.items.length === 0 || items.length >= data.total) return items
  }
}

/** GET {e}/campanas: AdminEmpresa ve todas las de su empresa. */
export async function listarCampanas(paginacion: Paginacion): Promise<PaginaDto<CampanaDto>> {
  const { data } = await apiClient.get<PaginaDto<CampanaDto>>(rutaEmpresa('/campanas'), { params: paginacion })
  return data
}

/** El backend no tiene GET {e}/campanas/{id}: se busca en el listado (mismo filtro por empresa y rol). */
export async function obtenerCampana(id: string): Promise<CampanaDto> {
  const campana = (await leerTodo<CampanaDto>(rutaEmpresa('/campanas'))).find((c) => c.id === id)
  if (!campana) throw new Error('La campaña no existe.')
  return campana
}

/** POST {e}/campanas → 201 CampanaDto. */
export async function crearCampana(dto: GuardarCampanaDto): Promise<CampanaDto> {
  const { data } = await apiClient.post<CampanaDto>(rutaEmpresa('/campanas'), dto)
  return data
}

/** PUT {e}/campanas/{id} → 204. */
export async function actualizarCampana(id: string, dto: GuardarCampanaDto): Promise<void> {
  await apiClient.put(rutaEmpresa(`/campanas/${id}`), dto)
}

/** GET {e}/campanas/{id}/usuarios: agentes y supervisores asignados, ordenados por nombre. */
export async function listarAsignados(campanaId: string): Promise<UsuarioDto[]> {
  return leerTodo<UsuarioDto>(rutaEmpresa(`/campanas/${campanaId}/usuarios`))
}

/** Usuarios activos con rol asignable; el backend rechaza los demás (ROL_NO_ASIGNABLE, USUARIO_INACTIVO). */
export async function listarCandidatos(): Promise<UsuarioDto[]> {
  const usuarios = await leerTodo<UsuarioDto>(rutaEmpresa('/usuarios'))
  return usuarios.filter((u) => u.activo && (ROLES_ASIGNABLES as readonly string[]).includes(u.rol))
}

/** PUT {e}/campanas/{id}/usuarios/{usuarioId} → 204 (idempotente). 403 LIMITE_PLAN_SUPERVISORES si el plan no admite más. */
export async function asignarUsuario(campanaId: string, usuarioId: string): Promise<void> {
  await apiClient.put(rutaEmpresa(`/campanas/${campanaId}/usuarios/${usuarioId}`))
}

/** DELETE {e}/campanas/{id}/usuarios/{usuarioId} → 204 (idempotente). */
export async function quitarUsuario(campanaId: string, usuarioId: string): Promise<void> {
  await apiClient.delete(rutaEmpresa(`/campanas/${campanaId}/usuarios/${usuarioId}`))
}
