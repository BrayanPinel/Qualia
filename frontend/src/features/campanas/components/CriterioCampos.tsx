import type { FieldErrors, UseFormRegister } from 'react-hook-form'
import { Boton } from '../../../shared/components/Boton'
import { CampoAreaTexto, CampoCasilla, CampoTexto } from '../../../shared/components/Formulario'
import { IconoX } from '../../../shared/components/Iconos'
import type { RubricaFormulario } from '../schemas/rubrica.schema'

interface PropsCriterioCampos {
  indice: number
  totalCriterios: number
  register: UseFormRegister<RubricaFormulario>
  errors: FieldErrors<RubricaFormulario>
  puedeQuitar: boolean
  onQuitar: () => void
}

/** Un criterio del editor de rúbrica (una fila de useFieldArray). */
export function CriterioCampos({ indice, totalCriterios, register, errors, puedeQuitar, onQuitar }: PropsCriterioCampos) {
  const error = errors.criterios?.[indice]
  const campo = <K extends keyof RubricaFormulario['criterios'][number]>(nombre: K) => `criterios.${indice}.${nombre}` as const
  // Al cambiar este nombre se revalidan los demás: así aparece o desaparece el aviso de duplicado en todos.
  const otrosNombres = Array.from({ length: totalCriterios }, (_, j) => `criterios.${j}.nombre` as const).filter((_, j) => j !== indice)

  return (
    <fieldset className="flex flex-col gap-4 rounded-lg border border-borde p-4">
      <div className="flex items-center justify-between gap-2">
        <legend className="text-sm font-medium">Criterio {indice + 1}</legend>
        <Boton
          variante="fantasma"
          tamano="sm"
          icono={<IconoX tamano={18} />}
          onClick={onQuitar}
          disabled={!puedeQuitar}
          aria-label={`Quitar el criterio ${indice + 1}`}
        >
          Quitar
        </Boton>
      </div>
      <CampoTexto etiqueta="Nombre" autoComplete="off" error={error?.nombre?.message} {...register(campo('nombre'), { deps: otrosNombres })} />
      <CampoAreaTexto etiqueta="Descripción" rows={2} error={error?.descripcion?.message} {...register(campo('descripcion'))} />
      <div className="grid gap-4 sm:grid-cols-2">
        <CampoTexto
          etiqueta="Peso"
          type="number"
          inputMode="decimal"
          step="0.01"
          ayuda="Mayor que 0 y hasta 100."
          error={error?.peso?.message}
          {...register(campo('peso'), { valueAsNumber: true })}
        />
        <CampoTexto
          etiqueta="Umbral de cumplimiento"
          type="number"
          inputMode="decimal"
          step="0.01"
          ayuda="Puntaje mínimo (0 a 100) para que el criterio se cumpla."
          error={error?.umbralCumplimiento?.message}
          {...register(campo('umbralCumplimiento'), { valueAsNumber: true })}
        />
      </div>
      <CampoCasilla
        etiqueta="Criterio crítico"
        ayuda="Si no se cumple, la evaluación se marca con falla crítica."
        error={error?.esCritico?.message}
        {...register(campo('esCritico'))}
      />
    </fieldset>
  )
}
