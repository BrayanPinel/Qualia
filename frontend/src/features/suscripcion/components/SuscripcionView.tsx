import { useState } from 'react'
import { obtenerPlanes } from '../../../shared/api/planesApi'
import { EncabezadoPagina } from '../../../shared/components/EncabezadoPagina'
import { EstadoCarga, EstadoError } from '../../../shared/components/EstadosVista'
import { useConsulta } from '../../../shared/hooks/useConsulta'
import { obtenerSuscripcion } from '../api/suscripcionApi'
import { ConsumoCard } from './ConsumoCard'
import { PagoSimuladoForm } from './PagoSimuladoForm'
import { PlanActualCard } from './PlanActualCard'

/** Suscripción de la empresa (AdminEmpresa): plan, estado y vigencia; pago simulado y consumo de minutos. */
export function SuscripcionView() {
  const [version, setVersion] = useState(0)
  const suscripcion = useConsulta(`suscripcion-${version}`, obtenerSuscripcion)
  const planes = useConsulta('planes', obtenerPlanes)

  const alRegistrarPago = () => setVersion((v) => v + 1)
  const error = suscripcion.error ?? planes.error

  return (
    <>
      <EncabezadoPagina titulo="Suscripción" descripcion="Plan contratado, vigencia y consumo de minutos de tu empresa." />

      {error && (
        <EstadoError
          mensaje={error}
          onReintentar={() => {
            suscripcion.recargar()
            planes.recargar()
          }}
        />
      )}
      {!error && !suscripcion.datos && <EstadoCarga texto="Cargando suscripción…" />}
      {!error && suscripcion.datos && (
        <div className="flex flex-col gap-6">
          <div className="grid gap-6 lg:grid-cols-2">
            <PlanActualCard
              suscripcion={suscripcion.datos}
              plan={planes.datos?.find((p) => p.id === suscripcion.datos?.planId)}
            />
            <PagoSimuladoForm onRegistrado={alRegistrarPago} />
          </div>
          <ConsumoCard claveRecarga={version} />
        </div>
      )}
    </>
  )
}
