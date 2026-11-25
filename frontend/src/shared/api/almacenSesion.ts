import type { SesionActiva } from '../types/comunes.types'

// Única fuente de la sesión en el navegador (localStorage). La leen el interceptor de Axios,
// rutaEmpresa y el SesionProvider; este último se suscribe para enterarse de un cierre forzado (401).

const CLAVE = 'qualia.sesion'

type Oyente = (sesion: SesionActiva | null) => void
const oyentes = new Set<Oyente>()

function esSesionValida(valor: unknown): valor is SesionActiva {
  const s = valor as SesionActiva | null
  return Boolean(s && typeof s.accessToken === 'string' && typeof s.expiraEn === 'string' && s.rol && s.usuario)
}

export function sesionVencida(sesion: SesionActiva): boolean {
  return new Date(sesion.expiraEn).getTime() <= Date.now()
}

/** Sesión vigente o null (si no hay, está dañada o ya venció). */
export function leerSesion(): SesionActiva | null {
  try {
    const valor: unknown = JSON.parse(localStorage.getItem(CLAVE) ?? 'null')
    if (!esSesionValida(valor) || sesionVencida(valor)) return null
    return valor
  } catch {
    return null
  }
}

export function guardarSesion(sesion: SesionActiva): void {
  localStorage.setItem(CLAVE, JSON.stringify(sesion))
  oyentes.forEach((o) => o(sesion))
}

export function borrarSesion(): void {
  localStorage.removeItem(CLAVE)
  oyentes.forEach((o) => o(null))
}

export function suscribirseASesion(oyente: Oyente): () => void {
  oyentes.add(oyente)
  return () => oyentes.delete(oyente)
}
