import type { InputHTMLAttributes } from 'react'
import { obtenerPlanes } from '../../../shared/api/planesApi'
import { EstadoCarga, EstadoError } from '../../../shared/components/EstadosVista'
import { useConsulta } from '../../../shared/hooks/useConsulta'
import { ETIQUETA_FUNCION_PLAN, type CodigoFuncionPlan } from '../../../shared/types/suscripciones.types'
import { formatearNumero, formatearPrecio } from '../../../shared/utils/formato'

interface PropsSelectorPlan extends InputHTMLAttributes<HTMLInputElement> {
  error?: string
}

/** Grupo de radios con el catálogo real de GET /api/planes. Recibe el register('planId') del formulario. */
export function SelectorPlan({ error, ...registro }: PropsSelectorPlan) {
  const { datos: planes, error: errorCarga, cargando, recargar } = useConsulta('planes', obtenerPlanes)

  return (
    <fieldset aria-describedby={error ? 'plan-error' : undefined}>
      <legend className="text-sm font-medium">Plan</legend>
      <p className="mt-1 text-meta text-texto-secundario">La empresa empieza con una suscripción de prueba del plan elegido.</p>

      {cargando && !planes && <EstadoCarga texto="Cargando planes…" />}
      {errorCarga && <EstadoError mensaje={errorCarga} onReintentar={recargar} />}

      {planes && (
        <div className="mt-3 grid gap-3 sm:grid-cols-3">
          {planes.map((plan) => (
            <label
              key={plan.id}
              className="relative flex cursor-pointer flex-col gap-2 rounded-lg border border-borde bg-superficie p-4 transition-colors hover:border-texto-secundario has-checked:border-oliva has-checked:bg-piedra has-focus-visible:outline-2 has-focus-visible:outline-offset-2 has-focus-visible:outline-oliva"
            >
              <input type="radio" value={plan.id} className="sr-only" {...registro} />
              <span className="font-titulo text-h3 font-semibold">{plan.nombre}</span>
              <span className="text-sm tabular-nums">
                <span className="font-semibold">{formatearPrecio(plan.precioMensual)}</span>
                <span className="text-texto-secundario"> / mes</span>
              </span>
              <span className="text-meta text-texto-secundario tabular-nums">
                {formatearNumero(plan.minutosIncluidos)} minutos incluidos
              </span>
              <ul className="mt-1 flex flex-col gap-1 border-t border-borde pt-2 text-meta">
                {plan.funciones.map((codigo) => (
                  <li key={codigo}>{ETIQUETA_FUNCION_PLAN[codigo as CodigoFuncionPlan] ?? codigo}</li>
                ))}
              </ul>
            </label>
          ))}
        </div>
      )}

      {error && (
        <p id="plan-error" className="mt-2 text-meta font-medium text-error">
          {error}
        </p>
      )}
    </fieldset>
  )
}
