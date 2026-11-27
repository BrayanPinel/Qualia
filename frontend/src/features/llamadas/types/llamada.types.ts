import type { EstadoEvaluacion, EstadoLlamada } from '../../../shared/types/comunes.types'

// Espejo exacto de LlamadasDtos.cs (camelCase). Los decimales de C# llegan como number.

/** LlamadaDto. requiereRevision es null mientras la llamada no tiene evaluación. */
export interface LlamadaDto {
  id: string
  empresaId: string
  campanaId: string
  agenteId: string
  rubricaId: string
  versionRubrica: number
  duracionSeg: number
  estado: EstadoLlamada
  fechaLlamada: string
  motivoFallo: string | null
  requiereRevision: boolean | null
}

/** SegmentoDto: hablante "Agente" o "Cliente"; el texto ya llega enmascarado. */
export interface SegmentoDto {
  id: string
  empresaId: string
  orden: number
  hablante: string
  inicioSeg: number
  finSeg: number
  textoRedactado: string
}

/** AccesoAudioDto: URL firmada y temporal (10 min). */
export interface AccesoAudioDto {
  url: string
  expiraEn: string
}

export interface EvaluacionAceptadaDto {
  llamadaId: string
  empresaId: string
  estado: EstadoLlamada
  seguimientoUrl: string
}

export type ModoIA = 'Real' | 'Simulado'

export interface EvidenciaDto {
  segmentoId: string
  citaTextual: string
  inicioSeg: number
  finSeg: number
  hablante: string
  textoSegmento: string
}

export interface ResultadoCriterioDto {
  criterioId: string
  nombre: string
  peso: number
  puntajeIA: number
  puntajeHumano: number | null
  puntajeVigente: number
  umbralCumplimiento: number
  cumple: boolean
  justificacion: string
  falloCritico: boolean
  evidencias: EvidenciaDto[]
}

export interface EvaluacionDto {
  id: string
  empresaId: string
  llamadaId: string
  estado: EstadoEvaluacion
  revision: number
  modoIA: ModoIA
  modeloUsado: string
  requiereRevision: boolean
  oficial: boolean
  puntajeTotal: number
  criterios: ResultadoCriterioDto[]
}

/** Query de GET {e}/llamadas (FiltroLlamadasDto). Los opcionales vacíos no se envían. */
export interface FiltroLlamadas {
  pagina: number
  tamanoPagina: number
  campanaId?: string
  agenteId?: string
  estado?: EstadoLlamada
  requiereRevision?: boolean
}

/** Valores de los selectores de filtro ('' = sin filtro). */
export interface SeleccionFiltros {
  campanaId: string
  agenteId: string
  estado: EstadoLlamada | ''
}

export const SIN_FILTROS: SeleccionFiltros = { campanaId: '', agenteId: '', estado: '' }

/** Agente que se puede elegir al cargar una llamada o mostrar en la tabla. */
export interface AgenteOpcion {
  id: string
  nombre: string
}

export const ESTADOS_LLAMADA: readonly EstadoLlamada[] = [
  'Disponible',
  'EnCola',
  'Transcribiendo',
  'Transcrita',
  'Evaluando',
  'EvaluadaPorIA',
  'Fallida',
]

/** Estados intermedios del worker: mientras dure alguno, el detalle consulta el estado periódicamente. */
export const ESTADOS_EN_PROCESO: readonly EstadoLlamada[] = ['EnCola', 'Transcribiendo', 'Transcrita', 'Evaluando']

export const ETIQUETA_ESTADO_LLAMADA: Record<EstadoLlamada, string> = {
  Disponible: 'Disponible',
  EnCola: 'En cola',
  Transcribiendo: 'Transcribiendo',
  Transcrita: 'Transcrita',
  Evaluando: 'Evaluando',
  EvaluadaPorIA: 'Evaluada por IA',
  Fallida: 'Fallida',
}

export const ETIQUETA_ESTADO_EVALUACION: Record<EstadoEvaluacion, string> = {
  EvaluadaPorIA: 'Evaluada por IA',
  ConfirmadaPorSupervisor: 'Confirmada por supervisor',
  AjustadaPorSupervisor: 'Ajustada por supervisor',
}
