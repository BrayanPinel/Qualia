import { z } from 'zod'

// Espejo de FiltroScorecardValidator, FiltroScorecardsCampanaValidator y FiltroReporteValidator.

export const filtroDesempenoSchema = z.object({
  campanaId: z.string().min(1, 'La campaña es obligatoria.'),
  periodo: z.string().regex(/^\d{4}-(0[1-9]|1[0-2])$/, 'El periodo debe tener el formato YYYY-MM.'),
})

export type FiltroDesempenoFormulario = z.infer<typeof filtroDesempenoSchema>

/** Mes UTC en curso, igual que el valor por defecto del backend. */
export const periodoActual = () => new Date().toISOString().slice(0, 7)
