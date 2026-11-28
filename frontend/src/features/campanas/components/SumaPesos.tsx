import { useWatch, type Control } from 'react-hook-form'
import { formatearNumero } from '../../../shared/utils/formato'
import { sumaEsValida, sumaPesos, type RubricaFormulario } from '../schemas/rubrica.schema'
import { SUMA_PESOS_REQUERIDA } from '../types/rubrica.types'

/** Suma de pesos en vivo. El borrador se guarda con cualquier suma; publicar exige exactamente 100. */
export function SumaPesos({ control }: { control: Control<RubricaFormulario> }) {
  const criterios = useWatch({ control, name: 'criterios' }) ?? []
  const suma = sumaPesos(criterios.map((c) => c?.peso))
  const valida = sumaEsValida(suma)
  const diferencia = Math.abs(SUMA_PESOS_REQUERIDA - suma)

  return (
    <div
      role="status"
      aria-live="polite"
      className={`flex flex-wrap items-baseline justify-between gap-2 rounded-lg px-4 py-3 ${valida ? 'bg-exito-fondo text-exito' : 'bg-alerta-fondo text-alerta'}`}
    >
      <p className="text-sm font-medium">
        Suma de pesos: <span className="tabular-nums">{formatearNumero(suma)}</span> / {SUMA_PESOS_REQUERIDA}
      </p>
      <p className="text-meta">
        {valida
          ? 'Lista para publicar.'
          : `${suma < SUMA_PESOS_REQUERIDA ? 'Faltan' : 'Sobran'} ${formatearNumero(diferencia)} para publicar.`}
      </p>
    </div>
  )
}
