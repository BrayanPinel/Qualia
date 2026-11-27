import { apiClient } from '../../../shared/api/apiClient'
import { rutaEmpresa } from '../../../shared/api/rutaEmpresa'
import type { PaginaDto, UsuarioDto } from '../../../shared/types/comunes.types'
import type { CargarLlamadaFormulario } from '../schemas/cargarLlamada.schema'
import type {
  AccesoAudioDto,
  AgenteOpcion,
  EvaluacionAceptadaDto,
  EvaluacionDto,
  FiltroLlamadas,
  LlamadaDto,
  SegmentoDto,
} from '../types/llamada.types'
import { idCorto } from '../utils/tiempo'

/** GET {e}/llamadas: el backend filtra por rol (Agente: las suyas; Supervisor: sus campañas). */
export async function listarLlamadas(filtro: FiltroLlamadas): Promise<PaginaDto<LlamadaDto>> {
  const { data } = await apiClient.get<PaginaDto<LlamadaDto>>(rutaEmpresa('/llamadas'), { params: filtro })
  return data
}

/** GET {e}/llamadas/{id}: detalle y estado del procesamiento. */
export async function obtenerLlamada(id: string): Promise<LlamadaDto> {
  const { data } = await apiClient.get<LlamadaDto>(rutaEmpresa(`/llamadas/${id}`))
  return data
}

/** POST {e}/llamadas/{id}/evaluacion → 202. Solo el Supervisor asignado; también reintenta una Fallida. */
export async function iniciarEvaluacion(id: string): Promise<EvaluacionAceptadaDto> {
  const { data } = await apiClient.post<EvaluacionAceptadaDto>(rutaEmpresa(`/llamadas/${id}/evaluacion`))
  return data
}

/** GET {e}/llamadas/{id}/transcripcion: lista vacía mientras no se transcribe. */
export async function obtenerTranscripcion(id: string): Promise<SegmentoDto[]> {
  const { data } = await apiClient.get<SegmentoDto[]>(rutaEmpresa(`/llamadas/${id}/transcripcion`))
  return data
}

/** GET {e}/llamadas/{id}/audio: URL firmada que vence a los 10 minutos (el acceso queda auditado). */
export async function obtenerAccesoAudio(id: string): Promise<AccesoAudioDto> {
  const { data } = await apiClient.get<AccesoAudioDto>(rutaEmpresa(`/llamadas/${id}/audio`))
  return data
}

/** GET {e}/llamadas/{id}/evaluacion: 404 EVALUACION_NO_DISPONIBLE si aún no hay evaluación. */
export async function obtenerEvaluacion(id: string): Promise<EvaluacionDto> {
  const { data } = await apiClient.get<EvaluacionDto>(rutaEmpresa(`/llamadas/${id}/evaluacion`))
  return data
}

/** POST {e}/llamadas (multipart). 403 CARGA_MANUAL_DESHABILITADA fuera de Development. */
export async function cargarLlamada(datos: CargarLlamadaFormulario): Promise<LlamadaDto> {
  const formulario = new FormData()
  formulario.append('audio', datos.audio)
  formulario.append('campanaId', datos.campanaId)
  formulario.append('agenteId', datos.agenteId)
  const { data } = await apiClient.post<LlamadaDto>(rutaEmpresa('/llamadas'), formulario, {
    headers: { 'Content-Type': 'multipart/form-data' },
  })
  return data
}

function agentesActivos(usuarios: UsuarioDto[]): AgenteOpcion[] {
  return usuarios.filter((u) => u.rol === 'Agente').map((u) => ({ id: u.id, nombre: u.nombre }))
}

/** GET {e}/usuarios (solo AdminEmpresa): agentes de la empresa, para mostrar su nombre en la tabla. */
export async function listarAgentesEmpresa(): Promise<AgenteOpcion[]> {
  const { data } = await apiClient.get<PaginaDto<UsuarioDto>>(rutaEmpresa('/usuarios'), {
    params: { pagina: 1, tamanoPagina: 100 },
  })
  return agentesActivos(data.items)
}

/**
 * GET {e}/campanas/{id}/scorecards: el Supervisor no puede listar usuarios, pero sí los agentes de sus campañas
 * a través de sus scorecards. Esa respuesta no trae el nombre, por eso se muestra el identificador.
 */
export async function listarAgentesDeCampanaSupervisor(campanaId: string): Promise<AgenteOpcion[]> {
  const { data } = await apiClient.get<PaginaDto<{ agenteId: string }>>(rutaEmpresa(`/campanas/${campanaId}/scorecards`), {
    params: { pagina: 1, tamanoPagina: 100 },
  })
  return data.items.map((s) => ({ id: s.agenteId, nombre: `Agente ${idCorto(s.agenteId)}` }))
}

/** GET {e}/campanas/{id}/usuarios (solo AdminEmpresa): agentes asignados a la campaña. */
export async function listarAgentesDeCampana(campanaId: string): Promise<AgenteOpcion[]> {
  const { data } = await apiClient.get<PaginaDto<UsuarioDto>>(rutaEmpresa(`/campanas/${campanaId}/usuarios`), {
    params: { pagina: 1, tamanoPagina: 100 },
  })
  return agentesActivos(data.items.filter((u) => u.activo))
}
