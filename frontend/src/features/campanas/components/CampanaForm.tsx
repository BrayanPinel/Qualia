import { useState } from 'react'
import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { aplicarErroresAlFormulario } from '../../../shared/api/extraerMensajeErrorBackend'
import { Boton } from '../../../shared/components/Boton'
import { AlertaFormulario, CampoCasilla, CampoTexto } from '../../../shared/components/Formulario'
import { actualizarCampana, crearCampana } from '../api/campanasApi'
import { guardarCampanaSchema, type GuardarCampanaFormulario } from '../schemas/campana.schema'
import type { CampanaDto } from '../types/campana.types'

const CAMPOS = ['nombre', 'activa'] as const

interface PropsCampanaForm {
  /** Sin campaña se crea una nueva; con campaña se edita. */
  campana?: CampanaDto
  onGuardada: () => void
  onCancelar: () => void
}

/** Crear o editar una campaña: nombre y si está activa. */
export function CampanaForm({ campana, onGuardada, onCancelar }: PropsCampanaForm) {
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isValid, isDirty, isSubmitting },
  } = useForm<GuardarCampanaFormulario>({
    resolver: zodResolver(guardarCampanaSchema),
    mode: 'onChange',
    defaultValues: { nombre: campana?.nombre ?? '', activa: campana?.activa ?? true },
  })

  const enviar = async (datos: GuardarCampanaFormulario) => {
    setErrorGeneral(null)
    try {
      if (campana) await actualizarCampana(campana.id, datos)
      else await crearCampana(datos)
      onGuardada()
    } catch (error) {
      setErrorGeneral(aplicarErroresAlFormulario(error, setError, CAMPOS))
    }
  }

  return (
    <form onSubmit={handleSubmit(enviar)} noValidate className="flex flex-col gap-5">
      <AlertaFormulario mensaje={errorGeneral} />
      <CampoTexto etiqueta="Nombre" autoComplete="off" error={errors.nombre?.message} {...register('nombre')} />
      <CampoCasilla
        etiqueta="Campaña activa"
        ayuda="Solo las campañas activas reciben llamadas nuevas."
        error={errors.activa?.message}
        {...register('activa')}
      />
      <div className="flex justify-end gap-2 border-t border-borde pt-5">
        <Boton variante="secundario" onClick={onCancelar}>
          Cancelar
        </Boton>
        <Boton type="submit" cargando={isSubmitting} disabled={!isValid || (campana !== undefined && !isDirty)}>
          {campana ? 'Guardar cambios' : 'Crear campaña'}
        </Boton>
      </div>
    </form>
  )
}
