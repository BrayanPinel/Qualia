import { useState } from 'react'
import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { Boton } from '../../../shared/components/Boton'
import { EstadoCarga, EstadoError } from '../../../shared/components/EstadosVista'
import { CampoTexto } from '../../../shared/components/Formulario'
import { Tarjeta } from '../../../shared/components/Tarjeta'
import { useConsulta } from '../../../shared/hooks/useConsulta'
import { obtenerConsumo } from '../api/suscripcionApi'
import { periodoConsumoSchema, type PeriodoConsumoFormulario } from '../schemas/suscripcion.schema'
import { BarraUso } from './BarraUso'

/** Consumo de minutos del periodo: contratados, reservados, facturados, disponibles y excedentes. */
export function ConsumoCard({ claveRecarga }: { claveRecarga: number }) {
  const [periodo, setPeriodo] = useState(() => new Date().toISOString().slice(0, 7))
  const { datos, error, cargando, recargar } = useConsulta(`consumo-${periodo}-${claveRecarga}`, () => obtenerConsumo(periodo))
  const {
    register,
    handleSubmit,
    formState: { errors, isValid },
  } = useForm<PeriodoConsumoFormulario>({
    resolver: zodResolver(periodoConsumoSchema),
    mode: 'onChange',
    defaultValues: { periodo },
  })

  const filtro = (
    <form onSubmit={handleSubmit((d) => setPeriodo(d.periodo))} noValidate className="flex items-start gap-2">
      <CampoTexto etiqueta="Periodo" type="month" error={errors.periodo?.message} {...register('periodo')} />
      <Boton type="submit" variante="secundario" disabled={!isValid} cargando={cargando && Boolean(datos)} className="mt-6.5">
        Consultar
      </Boton>
    </form>
  )

  return (
    <Tarjeta titulo="Consumo de minutos" descripcion="Minutos del periodo mensual (UTC)." acciones={filtro}>
      {error && <EstadoError mensaje={error} onReintentar={recargar} />}
      {!error && !datos && <EstadoCarga texto="Cargando consumo…" />}
      {!error && datos && <BarraUso consumo={datos} />}
    </Tarjeta>
  )
}
