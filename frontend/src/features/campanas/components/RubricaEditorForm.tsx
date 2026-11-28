import { useState } from 'react'
import { zodResolver } from '@hookform/resolvers/zod'
import { useFieldArray, useForm, useWatch } from 'react-hook-form'
import { aplicarErroresAlFormulario } from '../../../shared/api/extraerMensajeErrorBackend'
import { Boton } from '../../../shared/components/Boton'
import { AlertaFormulario, CampoTexto } from '../../../shared/components/Formulario'
import { IconoMas } from '../../../shared/components/Iconos'
import { actualizarRubrica, crearRubrica } from '../api/rubricasApi'
import { criterioVacio, rubricaSchema, sumaEsValida, sumaPesos, type RubricaFormulario } from '../schemas/rubrica.schema'
import { MAXIMO_CRITERIOS, MINIMO_CRITERIOS, type RubricaDto } from '../types/rubrica.types'
import { CriterioCampos } from './CriterioCampos'
import { PublicarRubricaBoton } from './PublicarRubricaBoton'
import { SumaPesos } from './SumaPesos'

const CAMPOS = ['nombre', 'criterios'] as const

interface PropsRubricaEditor {
  campanaId: string
  /** Borrador existente; sin él, el primer guardado crea la versión. */
  rubrica?: RubricaDto
  valoresIniciales: RubricaFormulario
  onGuardada: (rubrica: RubricaDto | null) => void
  onPublicada: () => void
}

/** Editor de un Borrador: nombre y de 1 a 30 criterios. Guardar no exige la suma de 100; publicar sí. */
export function RubricaEditorForm({ campanaId, rubrica, valoresIniciales, onGuardada, onPublicada }: PropsRubricaEditor) {
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null)
  const {
    control,
    register,
    handleSubmit,
    setError,
    formState: { errors, isValid, isDirty, isSubmitting },
  } = useForm<RubricaFormulario>({
    resolver: zodResolver(rubricaSchema),
    mode: 'onChange',
    defaultValues: valoresIniciales,
  })
  const { fields, append, remove } = useFieldArray({ control, name: 'criterios' })
  const criterios = useWatch({ control, name: 'criterios' }) ?? []
  const sumaCorrecta = sumaEsValida(sumaPesos(criterios.map((c) => c?.peso)))

  const enviar = async (datos: RubricaFormulario) => {
    setErrorGeneral(null)
    try {
      if (rubrica) {
        await actualizarRubrica(rubrica.id, datos)
        onGuardada(null)
      } else {
        onGuardada(await crearRubrica(campanaId, datos))
      }
    } catch (error) {
      setErrorGeneral(aplicarErroresAlFormulario(error, setError, CAMPOS))
    }
  }

  const errorLista = errors.criterios?.root?.message ?? errors.criterios?.message

  return (
    <form onSubmit={handleSubmit(enviar)} noValidate className="flex flex-col gap-6">
      <AlertaFormulario mensaje={errorGeneral} />
      <CampoTexto etiqueta="Nombre de la rúbrica" autoComplete="off" error={errors.nombre?.message} {...register('nombre')} />

      <SumaPesos control={control} />

      <div className="flex flex-col gap-4">
        {fields.map((campo, indice) => (
          <CriterioCampos
            key={campo.id}
            indice={indice}
            totalCriterios={fields.length}
            register={register}
            errors={errors}
            puedeQuitar={fields.length > MINIMO_CRITERIOS}
            onQuitar={() => remove(indice)}
          />
        ))}
        {errorLista && <p className="text-meta font-medium text-error">{errorLista}</p>}
        <Boton
          variante="secundario"
          icono={<IconoMas tamano={18} />}
          onClick={() => append(criterioVacio())}
          disabled={fields.length >= MAXIMO_CRITERIOS}
          className="self-start"
        >
          Agregar criterio ({fields.length}/{MAXIMO_CRITERIOS})
        </Boton>
      </div>

      <div className="flex flex-col gap-3 border-t border-borde pt-5 sm:flex-row sm:items-center sm:justify-end">
        <Boton type="submit" variante={rubrica ? 'secundario' : 'primario'} cargando={isSubmitting} disabled={!isValid || (rubrica && !isDirty)}>
          {rubrica ? 'Guardar borrador' : 'Crear borrador'}
        </Boton>
        {rubrica && (
          <PublicarRubricaBoton
            rubrica={rubrica}
            motivoBloqueo={
              isDirty ? 'Guarda los cambios antes de publicar.' : !sumaCorrecta ? 'Los pesos deben sumar exactamente 100.' : null
            }
            onPublicada={onPublicada}
          />
        )}
      </div>
    </form>
  )
}
