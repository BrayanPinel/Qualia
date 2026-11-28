import { z } from 'zod'
import { crearCriterioAjusteSchema, type CriterioAjusteFormulario } from '../../revision/schemas/ajuste.schema'

// Espejo de CrearDisputaValidator y ResolverDisputaValidator (Disputa.LongitudMinimaTexto = 10, LongitudMaximaTexto = 2000).
// Los criterios de "Modificar" usan las mismas reglas que el ajuste del supervisor.

function textoDisputa(campo: string, femenino: boolean) {
  const rango = `${campo} debe tener entre 10 y 2000 caracteres.`
  return z
    .string()
    .trim()
    .min(1, `${campo} es ${femenino ? 'obligatoria' : 'obligatorio'}.`)
    .min(10, rango)
    .max(2000, rango)
}

export const crearDisputaSchema = z.object({ motivo: textoDisputa('El motivo', false) })
export type CrearDisputaFormulario = z.infer<typeof crearDisputaSchema>

/**
 * Unión por decisión: "Mantener" valida solo la resolución (los criterios precargados se descartan);
 * "Modificar" exige además la lista completa de criterios válidos contra la transcripción.
 */
export function crearResolverDisputaSchema(textos: ReadonlyMap<string, string>) {
  const resolucion = textoDisputa('La resolución', true)
  return z.discriminatedUnion('decision', [
    z.object({ decision: z.literal('Mantener'), resolucion }),
    z.object({
      decision: z.literal('Modificar'),
      resolucion,
      criterios: z.array(crearCriterioAjusteSchema(textos)).min(1, 'Modificar exige la lista completa de criterios ajustados.'),
    }),
  ])
}

/** Valores del formulario: siempre traen los criterios para poder alternar la decisión sin perder lo escrito. */
export interface ResolverDisputaFormulario {
  decision: 'Mantener' | 'Modificar'
  resolucion: string
  criterios: CriterioAjusteFormulario[]
}
