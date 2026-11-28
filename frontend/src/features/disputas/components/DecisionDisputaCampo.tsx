import type { UseFormRegister } from 'react-hook-form'
import type { ResolverDisputaFormulario } from '../schemas/disputa.schema'

const OPCIONES = [
  { valor: 'Mantener', titulo: 'Mantener', detalle: 'Las notas no cambian.' },
  { valor: 'Modificar', titulo: 'Modificar', detalle: 'Ajustas los criterios con justificación y evidencia.' },
] as const

interface PropsDecision {
  register: UseFormRegister<ResolverDisputaFormulario>
  error?: string
}

/** Decisión de la disputa como dos opciones visibles (radio), no como selector. */
export function DecisionDisputaCampo({ register, error }: PropsDecision) {
  return (
    <fieldset className="flex flex-col gap-2">
      <legend className="mb-1.5 text-sm font-medium">Decisión</legend>
      <div className="grid gap-3 sm:grid-cols-2">
        {OPCIONES.map((o) => (
          <label
            key={o.valor}
            className="flex min-h-11 cursor-pointer items-start gap-3 rounded-lg border border-borde p-3 hover:bg-alabastro has-checked:border-oliva has-checked:bg-alabastro"
          >
            <input type="radio" value={o.valor} className="mt-0.5 size-5 accent-oliva" {...register('decision')} />
            <span className="flex flex-col">
              <span className="text-sm font-medium">{o.titulo}</span>
              <span className="text-meta text-texto-secundario">{o.detalle}</span>
            </span>
          </label>
        ))}
      </div>
      {error && <p className="text-meta font-medium text-error">{error}</p>}
    </fieldset>
  )
}
