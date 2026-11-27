// Formatos de tiempo y de identificadores del módulo de llamadas.

/** 75.4 → "1:15". Para tiempos de segmento y duración de la llamada. */
export function formatearSegundos(segundos: number): string {
  const total = Math.max(0, Math.floor(segundos))
  const minutos = Math.floor(total / 60)
  const resto = String(total % 60).padStart(2, '0')
  return `${minutos}:${resto}`
}

const fechaHora = new Intl.DateTimeFormat('es', { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' })

export const formatearFechaHora = (iso: string) => fechaHora.format(new Date(iso))

/** Últimos 8 caracteres de un Guid (los primeros se repiten entre registros), cuando el rol no puede consultar el nombre. */
export const idCorto = (id: string) => id.slice(-8)
