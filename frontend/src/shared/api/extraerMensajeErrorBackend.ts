import axios from 'axios'
import type { FieldValues, Path, UseFormSetError } from 'react-hook-form'
import type { ErrorDto } from '../types/comunes.types'

// Extractor universal de errores (Clase 16) adaptado al ErrorDto de Qualia:
// { status, codigo, mensaje, errores?, traceId }. Nunca se inventa un mensaje si el backend dio uno.

export const SIN_RESPUESTA = 'Sin respuesta del servidor (API apagada o bloqueo CORS).'

function esErrorDto(datos: unknown): datos is ErrorDto {
  const d = datos as ErrorDto | null
  return Boolean(d && typeof d === 'object' && typeof d.mensaje === 'string' && typeof d.codigo === 'string')
}

/** ErrorDto de la respuesta, o null si el error no vino del backend. */
export function leerErrorDto(error: unknown): ErrorDto | null {
  if (axios.isAxiosError(error) && esErrorDto(error.response?.data)) {
    return error.response.data
  }
  return null
}

/** "Password" o "Criterios[0].Peso" → "password" / "criterios.0.peso" (nombre del campo en React Hook Form). */
function aNombreDeCampo(clave: string): string {
  return clave
    .replace(/\[(\d+)\]/g, '.$1')
    .split('.')
    .map((parte) => parte.charAt(0).toLowerCase() + parte.slice(1))
    .join('.')
}

export function extraerMensajeErrorBackend(error: unknown): string {
  if (axios.isAxiosError(error)) {
    if (!error.response) return SIN_RESPUESTA

    const dto = leerErrorDto(error)
    if (dto) {
      const mensajes = Object.values(dto.errores ?? {}).flat()
      return mensajes.length > 0 ? mensajes.join(' ') : dto.mensaje
    }
    return `Error ${error.response.status}: ${error.message}`
  }
  return error instanceof Error ? error.message : 'Ocurrió un error inesperado.'
}

/**
 * Pinta bajo cada input los errores por campo del backend (400 VALIDACION).
 * Devuelve los mensajes que no corresponden a ningún campo del formulario, para mostrarlos arriba.
 */
export function aplicarErroresAlFormulario<T extends FieldValues>(
  error: unknown,
  setError: UseFormSetError<T>,
  camposDelFormulario: readonly string[],
): string | null {
  const dto = leerErrorDto(error)
  if (!dto?.errores) return extraerMensajeErrorBackend(error)

  const sueltos: string[] = []
  for (const [clave, mensajes] of Object.entries(dto.errores)) {
    const campo = aNombreDeCampo(clave)
    const raiz = campo.split('.')[0]
    if (camposDelFormulario.includes(raiz)) {
      setError(campo as Path<T>, { type: 'servidor', message: mensajes.join(' ') })
    } else {
      sueltos.push(...mensajes)
    }
  }
  return sueltos.length > 0 ? sueltos.join(' ') : null
}
