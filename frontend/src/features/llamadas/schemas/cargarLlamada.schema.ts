import { z } from 'zod'

// Espejo de CargarLlamadaValidator y de los límites de CargarLlamada (TamanoMaximoBytes y ExtensionesPermitidas).

export const EXTENSIONES_AUDIO = ['.ogg', '.opus', '.mp3', '.wav', '.m4a'] as const
export const TAMANO_MAXIMO_MB = 25
const TAMANO_MAXIMO_BYTES = TAMANO_MAXIMO_MB * 1024 * 1024

function extension(nombre: string): string {
  const punto = nombre.lastIndexOf('.')
  return punto >= 0 ? nombre.slice(punto).toLowerCase() : ''
}

export const cargarLlamadaSchema = z.object({
  campanaId: z.string().min(1, 'La campaña es obligatoria.'),
  agenteId: z.string().min(1, 'El agente es obligatorio.'),
  audio: z
    .instanceof(File, { message: 'El archivo de audio es obligatorio.' })
    .refine(
      (archivo) => (EXTENSIONES_AUDIO as readonly string[]).includes(extension(archivo.name)),
      `Formato no admitido. Formatos permitidos: ${EXTENSIONES_AUDIO.join(', ')}.`,
    )
    .refine((archivo) => archivo.size <= TAMANO_MAXIMO_BYTES, `El archivo no puede superar ${TAMANO_MAXIMO_MB} MB.`),
})

export type CargarLlamadaEntrada = z.input<typeof cargarLlamadaSchema>
export type CargarLlamadaFormulario = z.output<typeof cargarLlamadaSchema>
