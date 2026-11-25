import { z } from 'zod'
import { ESTADOS_SUSCRIPCION, type EstadoSuscripcion } from '../../../shared/types/suscripciones.types'

// Espejo de CambiarSuscripcionValidator. La fecha se elige como día (input date) y se envía
// en UTC al final de ese día, porque el backend exige DateTimeKind.Utc.

export const cambiarSuscripcionSchema = z.object({
  planId: z.string().min(1, 'El plan es obligatorio.'),
  estado: z
    .string()
    .refine(
      (valor) => ESTADOS_SUSCRIPCION.includes(valor as EstadoSuscripcion),
      'El estado debe ser uno de: Prueba, Activa, EnMora o Vencida.',
    )
    .transform((valor) => valor as EstadoSuscripcion),
  fechaVencimiento: z
    .string()
    .regex(/^\d{4}-\d{2}-\d{2}$/, 'La fecha de vencimiento es obligatoria.')
    .transform((dia) => `${dia}T23:59:59Z`),
  permiteExcedente: z.boolean(),
  motivo: z
    .string()
    .trim()
    .min(1, 'El motivo del cambio es obligatorio.')
    .min(10, 'El motivo debe tener entre 10 y 2000 caracteres.')
    .max(2000, 'El motivo debe tener entre 10 y 2000 caracteres.'),
})

export type CambiarSuscripcionEntrada = z.input<typeof cambiarSuscripcionSchema>
export type CambiarSuscripcionFormulario = z.output<typeof cambiarSuscripcionSchema>
