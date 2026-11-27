import { z } from 'zod'
import { RESULTADOS_PAGO, type ResultadoPagoSimulado } from '../types/suscripcion.types'

// Espejo de PagoSimuladoValidator: resultado obligatorio y exactamente "Aprobado" o "Rechazado".
export const pagoSimuladoSchema = z.object({
  resultado: z
    .string()
    .trim()
    .min(1, 'El resultado del pago simulado es obligatorio.')
    .refine(
      (valor) => RESULTADOS_PAGO.includes(valor as ResultadoPagoSimulado),
      "El resultado debe ser exactamente 'Aprobado' o 'Rechazado'.",
    )
    .transform((valor) => valor as ResultadoPagoSimulado),
})

export type PagoSimuladoEntrada = z.input<typeof pagoSimuladoSchema>
export type PagoSimuladoFormulario = z.output<typeof pagoSimuladoSchema>

// Espejo de ConsultarConsumoQueryValidator.
export const periodoConsumoSchema = z.object({
  periodo: z.string().regex(/^\d{4}-(0[1-9]|1[0-2])$/, 'El periodo debe tener formato YYYY-MM válido.'),
})

export type PeriodoConsumoFormulario = z.infer<typeof periodoConsumoSchema>
