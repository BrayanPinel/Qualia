import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { Boton } from '../../../shared/components/Boton'
import { CampoSelector, CampoTexto } from '../../../shared/components/Formulario'
import type { CampanaDto } from '../../../shared/types/campanas.types'
import { filtroDesempenoSchema, type FiltroDesempenoFormulario } from '../schemas/filtroDesempeno.schema'

interface PropsFiltro {
  campanas: CampanaDto[]
  valor: FiltroDesempenoFormulario
  consultando: boolean
  onConsultar: (filtro: FiltroDesempenoFormulario) => void
}

/** Campaña y mes (YYYY-MM) de la consulta. Valida mientras se escribe y bloquea "Consultar" si no es válido. */
export function FiltroDesempenoForm({ campanas, valor, consultando, onConsultar }: PropsFiltro) {
  const {
    register,
    handleSubmit,
    formState: { errors, isValid },
  } = useForm<FiltroDesempenoFormulario>({
    resolver: zodResolver(filtroDesempenoSchema),
    mode: 'onChange',
    defaultValues: valor,
  })

  return (
    <form
      onSubmit={handleSubmit(onConsultar)}
      noValidate
      className="grid items-start gap-4 sm:grid-cols-[2fr_1fr_auto]"
    >
      <CampoSelector etiqueta="Campaña" error={errors.campanaId?.message} {...register('campanaId')}>
        <option value="">Selecciona una campaña</option>
        {campanas.map((c) => (
          <option key={c.id} value={c.id}>
            {c.nombre}
          </option>
        ))}
      </CampoSelector>
      <CampoTexto etiqueta="Periodo" type="month" error={errors.periodo?.message} {...register('periodo')} />
      <Boton type="submit" variante="secundario" cargando={consultando} disabled={!isValid} className="sm:mt-6.5">
        Consultar
      </Boton>
    </form>
  )
}
