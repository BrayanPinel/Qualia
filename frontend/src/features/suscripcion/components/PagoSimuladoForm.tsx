import { useState } from 'react'
import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { aplicarErroresAlFormulario } from '../../../shared/api/extraerMensajeErrorBackend'
import { Boton } from '../../../shared/components/Boton'
import { AlertaFormulario } from '../../../shared/components/Formulario'
import { Tarjeta } from '../../../shared/components/Tarjeta'
import { registrarPagoSimulado } from '../api/suscripcionApi'
import { pagoSimuladoSchema, type PagoSimuladoEntrada, type PagoSimuladoFormulario } from '../schemas/suscripcion.schema'
import { RESULTADOS_PAGO } from '../types/suscripcion.types'

const CAMPOS = ['resultado'] as const

const DESCRIPCION_RESULTADO = {
  Aprobado: 'Deja la suscripción activa y vigente al menos hasta el fin del mes en curso.',
  Rechazado: 'No renueva la suscripción; si ya venció, pasa a mora.',
} as const

/** Pago simulado: no cobra nada, solo cambia el estado de la suscripción como lo haría la pasarela. */
export function PagoSimuladoForm({ onRegistrado }: { onRegistrado: () => void }) {
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null)
  const [exito, setExito] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    setError,
    reset,
    formState: { errors, isValid, isSubmitting },
  } = useForm<PagoSimuladoEntrada, unknown, PagoSimuladoFormulario>({
    resolver: zodResolver(pagoSimuladoSchema),
    mode: 'onChange',
    defaultValues: { resultado: '' },
  })

  const enviar = async (datos: PagoSimuladoFormulario) => {
    setErrorGeneral(null)
    setExito(null)
    try {
      const suscripcion = await registrarPagoSimulado(datos)
      setExito(`Pago aprobado. La suscripción queda en estado ${suscripcion.estado}.`)
      reset()
    } catch (error) {
      setErrorGeneral(aplicarErroresAlFormulario(error, setError, CAMPOS))
    } finally {
      // Aprobado o rechazado, el estado de la suscripción pudo cambiar.
      onRegistrado()
    }
  }

  return (
    <Tarjeta titulo="Pago simulado" descripcion="Elige el resultado que devolvería la pasarela de pago.">
      <form onSubmit={handleSubmit(enviar)} noValidate className="flex flex-col gap-5">
        <AlertaFormulario mensaje={errorGeneral} />
        {exito && (
          <p role="status" className="rounded-lg border border-exito/30 bg-exito-fondo px-4 py-3 text-sm text-exito">
            {exito}
          </p>
        )}
        <fieldset className="flex flex-col gap-3" aria-describedby={errors.resultado ? 'resultado-error' : undefined}>
          <legend className="mb-1.5 text-sm font-medium">Resultado del pago</legend>
          {RESULTADOS_PAGO.map((resultado) => (
            <label
              key={resultado}
              className="flex cursor-pointer items-start gap-3 rounded-lg border border-borde px-4 py-3 has-checked:border-oliva has-checked:bg-alabastro"
            >
              <input type="radio" value={resultado} className="mt-1 size-4 accent-oliva" {...register('resultado')} />
              <span>
                <span className="block text-sm font-medium">{resultado}</span>
                <span className="block text-meta text-texto-secundario">{DESCRIPCION_RESULTADO[resultado]}</span>
              </span>
            </label>
          ))}
          {errors.resultado && (
            <p id="resultado-error" className="text-meta font-medium text-error">
              {errors.resultado.message}
            </p>
          )}
        </fieldset>
        <div className="flex justify-end border-t border-borde pt-5">
          <Boton type="submit" cargando={isSubmitting} disabled={!isValid}>
            Registrar pago
          </Boton>
        </div>
      </form>
    </Tarjeta>
  )
}
