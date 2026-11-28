import { z } from 'zod'
import { nombreEntre3y100 } from '../../../shared/schemas/camposComunes'
import { MAXIMO_CRITERIOS, MINIMO_CRITERIOS, SUMA_PESOS_REQUERIDA } from '../types/rubrica.types'

// Espejo de CrearRubricaValidator y CriterioEntradaValidator. La suma de 100 no la exige el backend al guardar
// el borrador, solo al publicar (Rubrica.ErroresDePublicacion): aquí se calcula con sumaPesos() para mostrarla en vivo.

/** PrecisionScale(5, 2): como máximo dos decimales. */
const conDosDecimales = (valor: number) => /^\d+(\.\d{1,2})?$/.test(String(valor))

const MENSAJE_CANTIDAD = `La rúbrica debe tener entre ${MINIMO_CRITERIOS} y ${MAXIMO_CRITERIOS} criterios.`
const MENSAJE_NOMBRE_CRITERIO = 'El nombre del criterio debe tener entre 3 y 100 caracteres.'
const MENSAJE_UMBRAL = 'El umbral de cumplimiento debe estar entre 0 y 100.'

export const criterioSchema = z.object({
  nombre: z
    .string()
    .trim()
    .min(1, 'El nombre del criterio es obligatorio.')
    .min(3, MENSAJE_NOMBRE_CRITERIO)
    .max(100, MENSAJE_NOMBRE_CRITERIO),
  descripcion: z
    .string()
    .trim()
    .min(1, 'La descripción del criterio es obligatoria.')
    .max(2000, 'La descripción no puede superar 2,000 caracteres.'),
  // Un input numérico vacío llega como NaN (valueAsNumber) y Zod lo informa como "obligatorio".
  peso: z
    .number({ error: 'El peso es obligatorio.' })
    .gt(0, 'El peso debe ser positivo.')
    .lte(100, 'El peso no puede ser mayor que 100.')
    .refine(conDosDecimales, 'El peso admite como máximo dos decimales.'),
  esCritico: z.boolean({ error: 'Indique si el criterio es crítico.' }),
  umbralCumplimiento: z
    .number({ error: 'El umbral de cumplimiento es obligatorio.' })
    .min(0, MENSAJE_UMBRAL)
    .max(100, MENSAJE_UMBRAL)
    .refine(conDosDecimales, 'El umbral admite como máximo dos decimales.'),
})

export const rubricaSchema = z.object({
  nombre: nombreEntre3y100('El nombre es obligatorio.'),
  criterios: z
    .array(criterioSchema)
    .min(MINIMO_CRITERIOS, MENSAJE_CANTIDAD)
    .max(MAXIMO_CRITERIOS, MENSAJE_CANTIDAD)
    .superRefine(
      (criterios, ctx) => {
        // Rubrica.NombresDuplicados: sin distinguir mayúsculas ni espacios sobrantes. Se marcan todas las
        // apariciones para que el error se vea en el campo que se está editando.
        const clave = (c: unknown) => (typeof (c as CriterioFormulario)?.nombre === 'string' ? (c as CriterioFormulario).nombre.trim().toLowerCase() : '')
        const conteo = new Map<string, number>()
        criterios.forEach((c) => conteo.set(clave(c), (conteo.get(clave(c)) ?? 0) + 1))
        criterios.forEach((c, i) => {
          if (clave(c) && (conteo.get(clave(c)) ?? 0) > 1) {
            ctx.addIssue({ code: 'custom', message: 'Hay nombres de criterio duplicados dentro de la versión.', path: [i, 'nombre'] })
          }
        })
      },
      // Por defecto Zod omite la regla si otro campo del criterio tiene errores; aquí se evalúa siempre.
      { when: (payload) => Array.isArray(payload.value) },
    ),
})

export type RubricaFormulario = z.infer<typeof rubricaSchema>
export type CriterioFormulario = z.infer<typeof criterioSchema>

/** Suma de pesos en centésimas para evitar errores de coma flotante (33.33 + 33.33 + 33.34 = 100). */
export function sumaPesos(pesos: readonly (number | undefined)[]): number {
  const centesimas = pesos.reduce<number>((total, p) => total + (Number.isFinite(p) ? Math.round((p as number) * 100) : 0), 0)
  return centesimas / 100
}

export const sumaEsValida = (suma: number) => Math.round(suma * 100) === SUMA_PESOS_REQUERIDA * 100

/** Criterio vacío para "Agregar criterio". Los números arrancan en NaN para que el campo empiece vacío. */
export function criterioVacio(): CriterioFormulario {
  return { nombre: '', descripcion: '', peso: Number.NaN, esCritico: false, umbralCumplimiento: 70 }
}
