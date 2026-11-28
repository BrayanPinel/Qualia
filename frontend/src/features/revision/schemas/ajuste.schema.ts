import { z } from 'zod'
import { LONGITUD_MAXIMA_CITA } from '../types/revision.types'

// Espejo de AjustarEvaluacionValidator y AjusteCriterioValidator, más la regla RN-09 del caso de uso:
// la cita debe aparecer en el texto del segmento (ConstructorEvaluacion.EsCitaValida). Por eso el schema
// se arma con los segmentos de la llamada. También lo reutiliza la resolución de disputas ("Modificar").

const conDosDecimales = (valor: number) => /^\d+(\.\d{1,2})?$/.test(String(valor))

/** Igual que el backend: espacios colapsados y sin distinguir mayúsculas. */
const normalizar = (texto: string) => texto.trim().replace(/\s+/g, ' ').toLowerCase()

/** TextoJustificadoValidator: de 10 a 2,000 caracteres. */
export function textoJustificado(campo: string, femenino: boolean) {
  const rango = `${campo} debe tener entre 10 y 2,000 caracteres.`
  return z
    .string()
    .trim()
    .min(1, `${campo} es ${femenino ? 'obligatoria' : 'obligatorio'}.`)
    .min(10, rango)
    .max(2000, rango)
}

/** Criterio del ajuste, validado contra el texto de los segmentos de la llamada (id → texto enmascarado). */
export function crearCriterioAjusteSchema(textos: ReadonlyMap<string, string>) {
  const evidencia = z
    .object({
      segmentoId: z.string().min(1, 'El segmento es obligatorio.'),
      citaTextual: z
        .string()
        .trim()
        .min(1, 'La cita textual es obligatoria.')
        .max(LONGITUD_MAXIMA_CITA, `La cita no puede superar ${LONGITUD_MAXIMA_CITA} caracteres.`),
    })
    .superRefine((e, ctx) => {
      const texto = textos.get(e.segmentoId)
      if (texto === undefined) {
        ctx.addIssue({ code: 'custom', message: 'El segmento no pertenece a la transcripción de esta llamada.', path: ['segmentoId'] })
      } else if (!normalizar(texto).includes(normalizar(e.citaTextual))) {
        ctx.addIssue({ code: 'custom', message: 'La cita no aparece en el texto del segmento indicado.', path: ['citaTextual'] })
      }
    })

  return z.object({
    criterioId: z.string().min(1, 'El criterio es obligatorio.'),
    puntaje: z
      .number({ error: 'El puntaje es obligatorio.' })
      .min(0, 'El puntaje debe estar entre 0 y 100.')
      .max(100, 'El puntaje debe estar entre 0 y 100.')
      .refine(conDosDecimales, 'El puntaje admite como máximo dos decimales.'),
    justificacion: textoJustificado('La justificación', true),
    evidencias: z.array(evidencia).superRefine((evidencias, ctx) => {
      const segmentos = evidencias.map((e) => e.segmentoId)
      if (new Set(segmentos).size !== segmentos.length) {
        ctx.addIssue({ code: 'custom', message: 'Use una sola cita por segmento en cada criterio.' })
      }
    }),
  })
}

export function crearAjusteSchema(textos: ReadonlyMap<string, string>) {
  return z.object({
    motivo: textoJustificado('El motivo', false),
    criterios: z.array(crearCriterioAjusteSchema(textos)).min(1, 'El ajuste debe incluir los criterios de la rúbrica.'),
  })
}

export type CriterioAjusteFormulario = z.infer<ReturnType<typeof crearCriterioAjusteSchema>>
export type AjusteFormulario = z.infer<ReturnType<typeof crearAjusteSchema>>
