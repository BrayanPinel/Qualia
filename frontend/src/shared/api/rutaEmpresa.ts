import { leerSesion } from './almacenSesion'

/**
 * Arma una ruta de empresa con la empresa de la sesión: rutaEmpresa('/llamadas') → '/empresas/{empresaId}/llamadas'.
 * Nadie escribe el empresaId a mano: si no coincide con el del token, el backend responde 404.
 */
export function rutaEmpresa(ruta: string): string {
  const empresaId = leerSesion()?.empresaId
  if (!empresaId) {
    throw new Error('La sesión actual no pertenece a una empresa.')
  }
  const sufijo = ruta.startsWith('/') ? ruta : `/${ruta}`
  return `/empresas/${empresaId}${sufijo}`
}
