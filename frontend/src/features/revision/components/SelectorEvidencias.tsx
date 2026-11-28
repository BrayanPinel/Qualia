import { useState } from 'react'
import { useFieldArray, type ArrayPath, type Control, type FieldArray, type FieldErrors, type Path, type UseFormRegister } from 'react-hook-form'
import { Badge } from '../../../shared/components/Badge'
import { Boton } from '../../../shared/components/Boton'
import { CampoAreaTexto, CampoSelector } from '../../../shared/components/Formulario'
import { IconoMas, IconoX } from '../../../shared/components/Iconos'
import type { SegmentoDto } from '../../llamadas/types/llamada.types'
import { formatearSegundos } from '../../llamadas/utils/tiempo'
import type { CriterioAjusteFormulario } from '../schemas/ajuste.schema'
import { LONGITUD_MAXIMA_CITA } from '../types/revision.types'

type ConCriterios = { criterios: CriterioAjusteFormulario[] }

interface PropsSelectorEvidencias<T extends ConCriterios> {
  indice: number
  control: Control<T>
  register: UseFormRegister<T>
  errors: FieldErrors<T>
  segmentos: SegmentoDto[]
}

/**
 * Evidencias de un criterio: se eligen segmentos de la transcripción y se recorta la cita.
 * La cita debe aparecer en el texto del segmento; una cita por segmento. Sin evidencias, la justificación lo explica.
 */
export function SelectorEvidencias<T extends ConCriterios>({ indice, control, register, errors, segmentos }: PropsSelectorEvidencias<T>) {
  const { fields, append, remove } = useFieldArray({ control, name: `criterios.${indice}.evidencias` as ArrayPath<T> })
  const evidencias = fields as unknown as (CriterioAjusteFormulario['evidencias'][number] & { id: string })[]
  const errorCriterio = (errors as FieldErrors<ConCriterios>).criterios?.[indice]?.evidencias
  const usados = new Set(evidencias.map((e) => e.segmentoId))
  const porId = new Map(segmentos.map((s) => [s.id, s]))

  const [elegido, setElegido] = useState('')

  /** La cita arranca con el texto completo del segmento; el supervisor la recorta. */
  const agregar = () => {
    const segmento = porId.get(elegido)
    if (!segmento) return
    const evidencia = { segmentoId: segmento.id, citaTextual: segmento.textoRedactado.trim().slice(0, LONGITUD_MAXIMA_CITA) }
    append(evidencia as FieldArray<T, ArrayPath<T>>)
    setElegido('')
  }

  return (
    <div className="flex flex-col gap-3">
      <p className="text-sm font-medium">Evidencias ({evidencias.length})</p>
      {evidencias.map((e, j) => {
        const segmento = porId.get(e.segmentoId)
        const error = errorCriterio?.[j]
        return (
          <div key={e.id} className="flex flex-col gap-2 rounded-lg bg-alabastro p-3">
            <div className="flex items-center justify-between gap-2">
              <span className="flex items-center gap-2 text-meta text-texto-secundario">
                <Badge tono="neutro" sinIcono>{segmento?.hablante ?? 'Segmento'}</Badge>
                {segmento && <span className="tabular-nums">{formatearSegundos(segmento.inicioSeg)}</span>}
              </span>
              <Boton variante="fantasma" tamano="sm" icono={<IconoX tamano={16} />} onClick={() => remove(j)} aria-label={`Quitar la evidencia ${j + 1}`}>
                Quitar
              </Boton>
            </div>
            <CampoAreaTexto
              etiqueta="Cita textual"
              rows={2}
              ayuda="Recorta el texto del segmento a la parte que respalda la nota."
              error={error?.citaTextual?.message ?? error?.segmentoId?.message ?? error?.message}
              {...register(`criterios.${indice}.evidencias.${j}.citaTextual` as Path<T>)}
            />
          </div>
        )
      })}
      {(errorCriterio?.root?.message ?? errorCriterio?.message) && (
        <p className="text-meta font-medium text-error">{errorCriterio?.root?.message ?? errorCriterio?.message}</p>
      )}
      {/* Elegir y luego "Agregar": con teclado, cada flecha sobre un select dispara un cambio. */}
      <div className="flex flex-col gap-2 sm:flex-row sm:items-end">
        <div className="flex-1">
          <CampoSelector etiqueta="Agregar evidencia de un segmento" value={elegido} onChange={(evento) => setElegido(evento.target.value)}>
            <option value="">Selecciona un segmento de la transcripción</option>
            {segmentos.filter((s) => !usados.has(s.id)).map((s) => (
              <option key={s.id} value={s.id}>
                {formatearSegundos(s.inicioSeg)} · {s.hablante}: {s.textoRedactado.slice(0, 80)}
              </option>
            ))}
          </CampoSelector>
        </div>
        <Boton variante="secundario" icono={<IconoMas tamano={18} />} disabled={!elegido} onClick={agregar}>
          Agregar
        </Boton>
      </div>
    </div>
  )
}
