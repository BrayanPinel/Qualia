import type { Control, FieldErrors, Path, UseFormRegister } from 'react-hook-form'
import { BadgeCumplimiento } from '../../../shared/components/estadosBadge'
import { CampoAreaTexto, CampoTexto } from '../../../shared/components/Formulario'
import { formatearNumero } from '../../../shared/utils/formato'
import type { ResultadoCriterioDto, SegmentoDto } from '../../llamadas/types/llamada.types'
import type { CriterioAjusteFormulario } from '../schemas/ajuste.schema'
import { SelectorEvidencias } from './SelectorEvidencias'

type ConCriterios = { criterios: CriterioAjusteFormulario[] }

interface PropsCamposAjuste<T extends ConCriterios> {
  indice: number
  /** Resultado actual del criterio: nombre, peso, umbral y puntajes de referencia. */
  criterio: ResultadoCriterioDto
  control: Control<T>
  register: UseFormRegister<T>
  errors: FieldErrors<T>
  segmentos: SegmentoDto[]
}

/** Un criterio del ajuste: puntaje, justificación y evidencias. Lo reutiliza la resolución de disputas ("Modificar"). */
export function CamposAjusteCriterio<T extends ConCriterios>({ indice, criterio, control, register, errors, segmentos }: PropsCamposAjuste<T>) {
  const error = (errors as FieldErrors<ConCriterios>).criterios?.[indice]
  const campo = (nombre: 'puntaje' | 'justificacion') => `criterios.${indice}.${nombre}` as Path<T>

  return (
    <fieldset className="flex flex-col gap-4 rounded-lg border border-borde p-4">
      <legend className="sr-only">{criterio.nombre}</legend>
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div>
          <p className="font-medium">{criterio.nombre}</p>
          <p className="text-meta text-texto-secundario tabular-nums">
            Peso {formatearNumero(criterio.peso)} · Umbral {formatearNumero(criterio.umbralCumplimiento)} · IA {formatearNumero(criterio.puntajeIA)}
            {criterio.puntajeHumano !== null && ` · Supervisor ${formatearNumero(criterio.puntajeHumano)}`}
          </p>
        </div>
        <BadgeCumplimiento cumple={criterio.cumple} />
      </div>
      <CampoTexto
        etiqueta="Puntaje"
        type="number"
        inputMode="decimal"
        step="0.01"
        ayuda="De 0 a 100, hasta dos decimales."
        error={error?.puntaje?.message}
        {...register(campo('puntaje'), { valueAsNumber: true })}
      />
      <CampoAreaTexto etiqueta="Justificación" rows={3} error={error?.justificacion?.message} {...register(campo('justificacion'))} />
      <SelectorEvidencias indice={indice} control={control} register={register} errors={errors} segmentos={segmentos} />
    </fieldset>
  )
}
