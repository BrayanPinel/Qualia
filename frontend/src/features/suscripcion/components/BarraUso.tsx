import type { ConsumoDto } from '../../../shared/types/suscripciones.types'
import { formatearNumero } from '../../../shared/utils/formato'

function Cifra({ etiqueta, valor, marca }: { etiqueta: string; valor: number; marca?: string }) {
  return (
    <div className="flex flex-col gap-1">
      <dt className="flex items-center gap-2 text-meta text-texto-secundario">
        {marca && <span aria-hidden="true" className={`size-2.5 rounded-sm ${marca}`} />}
        {etiqueta}
      </dt>
      <dd className="text-h3 font-semibold tabular-nums">{formatearNumero(valor)}</dd>
    </div>
  )
}

const porcentaje = (parte: number, total: number) => (total > 0 ? Math.min(100, (parte / total) * 100) : 0)

/** Barra de uso: facturados + reservados sobre los minutos contratados del periodo. */
export function BarraUso({ consumo }: { consumo: ConsumoDto }) {
  const usados = consumo.minutosFacturables + consumo.minutosReservados
  const pctFacturado = porcentaje(consumo.minutosFacturables, consumo.minutosContratados)
  const pctReservado = Math.min(100 - pctFacturado, porcentaje(consumo.minutosReservados, consumo.minutosContratados))
  const pctUso = Math.round(porcentaje(usados, consumo.minutosContratados))

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-2">
        <div className="flex items-baseline justify-between text-sm">
          <span className="font-medium">Periodo {consumo.periodo}</span>
          <span className="text-texto-secundario tabular-nums">
            {formatearNumero(usados)} de {formatearNumero(consumo.minutosContratados)} min ({pctUso} %)
          </span>
        </div>
        <div
          role="progressbar"
          aria-label="Uso de minutos del periodo"
          aria-valuemin={0}
          aria-valuemax={100}
          aria-valuenow={pctUso}
          className="flex h-3 overflow-hidden rounded-full bg-piedra"
        >
          <div className="h-full bg-oliva" style={{ width: `${pctFacturado}%` }} />
          <div className="h-full bg-alerta" style={{ width: `${pctReservado}%` }} />
        </div>
      </div>

      <dl className="grid grid-cols-2 gap-5 md:grid-cols-5">
        <Cifra etiqueta="Contratados" valor={consumo.minutosContratados} />
        <Cifra etiqueta="Facturados" valor={consumo.minutosFacturables} marca="bg-oliva" />
        <Cifra etiqueta="Reservados" valor={consumo.minutosReservados} marca="bg-alerta" />
        <Cifra etiqueta="Disponibles" valor={consumo.minutosDisponibles} marca="bg-piedra" />
        <Cifra etiqueta="Excedentes" valor={consumo.minutosExcedentes} />
      </dl>
    </div>
  )
}
