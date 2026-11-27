import { BadgeEstadoSuscripcion } from '../../../shared/components/estadosBadge'
import { Tarjeta } from '../../../shared/components/Tarjeta'
import {
  ETIQUETA_FUNCION_PLAN,
  type CodigoFuncionPlan,
  type PlanDto,
  type SuscripcionDto,
} from '../../../shared/types/suscripciones.types'
import { formatearFecha, formatearNumero, formatearPrecio } from '../../../shared/utils/formato'

interface PropsPlanActual {
  suscripcion: SuscripcionDto
  plan: PlanDto | undefined
}

const limite = (valor: number | null | undefined) => (valor === null ? 'Sin límite' : valor === undefined ? '—' : String(valor))

/** Plan contratado, estado de la suscripción, vigencia y funciones incluidas. */
export function PlanActualCard({ suscripcion, plan }: PropsPlanActual) {
  return (
    <Tarjeta titulo={plan?.nombre ?? 'Plan contratado'} acciones={<BadgeEstadoSuscripcion estado={suscripcion.estado} />}>
      <div className="flex flex-col gap-5">
        {plan && (
          <p>
            <span className="font-titulo text-h2 font-bold tabular-nums">{formatearPrecio(plan.precioMensual)}</span>
            <span className="text-sm text-texto-secundario"> / mes</span>
          </p>
        )}
        <dl className="grid grid-cols-2 gap-4 text-sm">
          <div>
            <dt className="text-meta text-texto-secundario">Vence el</dt>
            <dd className="font-medium">{formatearFecha(suscripcion.fechaVencimiento)}</dd>
          </div>
          <div>
            <dt className="text-meta text-texto-secundario">Minutos incluidos</dt>
            <dd className="font-medium tabular-nums">{plan ? formatearNumero(plan.minutosIncluidos) : '—'}</dd>
          </div>
          <div>
            <dt className="text-meta text-texto-secundario">Supervisores</dt>
            <dd className="font-medium">{limite(plan?.limiteSupervisores)}</dd>
          </div>
          <div>
            <dt className="text-meta text-texto-secundario">Rúbricas activas</dt>
            <dd className="font-medium">{limite(plan?.limiteRubricas)}</dd>
          </div>
          <div>
            <dt className="text-meta text-texto-secundario">Minutos excedentes</dt>
            <dd className="font-medium">
              {suscripcion.permiteExcedente ? `Permitidos (${plan ? formatearPrecio(plan.precioExcedente) : '—'} / min)` : 'No permitidos'}
            </dd>
          </div>
        </dl>
        {plan && plan.funciones.length > 0 && (
          <ul className="flex flex-wrap gap-2">
            {plan.funciones.map((f) => (
              <li key={f} className="rounded-md bg-piedra px-2 py-0.5 text-meta">
                {ETIQUETA_FUNCION_PLAN[f as CodigoFuncionPlan] ?? f}
              </li>
            ))}
          </ul>
        )}
      </div>
    </Tarjeta>
  )
}
