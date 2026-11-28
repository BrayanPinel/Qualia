import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { EncabezadoPagina } from '../../../shared/components/EncabezadoPagina'
import { EstadoCarga, EstadoError } from '../../../shared/components/EstadosVista'
import { IconoFlechaIzq } from '../../../shared/components/Iconos'
import { Tarjeta } from '../../../shared/components/Tarjeta'
import { useConsulta } from '../../../shared/hooks/useConsulta'
import { listarRubricas } from '../api/rubricasApi'
import { criterioVacio, type RubricaFormulario } from '../schemas/rubrica.schema'
import { PARAMETRO_BASE, RUBRICA_NUEVA, type RubricaDto } from '../types/rubrica.types'
import { CLASE_ENLACE_ACCION } from '../utils/estilos'
import { BadgeEstadoRubrica } from './BadgesCampana'
import { RubricaEditorForm } from './RubricaEditorForm'
import { RubricaSoloLectura } from './RubricaSoloLectura'

/** Valores del formulario a partir de una versión (sin ids: el backend recrea los criterios). */
function aFormulario(r: RubricaDto): RubricaFormulario {
  return {
    nombre: r.nombre,
    criterios: r.criterios.map(({ nombre, descripcion, peso, esCritico, umbralCumplimiento }) => ({
      nombre, descripcion, peso, esCritico, umbralCumplimiento,
    })),
  }
}

/** /campanas/:id/rubricas/:rubricaId — "nueva" abre el editor (copiando la versión activa si existe). */
export function RubricaView() {
  const { id: campanaId = '', rubricaId = '' } = useParams()
  const [parametros] = useSearchParams()
  const navegar = useNavigate()
  const { datos, error, recargar } = useConsulta(`rubrica-${campanaId}-${rubricaId}`, () => listarRubricas(campanaId))

  const volver = (
    <Link to={`/campanas/${campanaId}`} className={CLASE_ENLACE_ACCION}>
      <IconoFlechaIzq tamano={18} />
      Volver a la campaña
    </Link>
  )

  if (error) {
    return (
      <>
        <EncabezadoPagina titulo="Rúbrica" acciones={volver} />
        <EstadoError mensaje={error} onReintentar={recargar} />
      </>
    )
  }
  if (!datos) return <EstadoCarga texto="Cargando rúbrica…" />

  if (rubricaId === RUBRICA_NUEVA) {
    // ?base=<id> restaura una versión anterior como nueva; sin él se parte de la activa (o de la más reciente).
    const idBase = parametros.get(PARAMETRO_BASE)
    const base = (idBase && datos.find((r) => r.id === idBase)) || datos.find((r) => r.estado === 'Activa') || datos[0]
    return (
      <>
        <EncabezadoPagina
          titulo={`Nueva versión (v${(datos[0]?.version ?? 0) + 1})`}
          descripcion={base ? `Parte de los criterios de la versión ${base.version}; ajústalos y crea el borrador.` : 'Define los criterios y crea el borrador.'}
          acciones={volver}
        />
        <Tarjeta>
          <RubricaEditorForm
            key={base?.id ?? 'vacia'}
            campanaId={campanaId}
            valoresIniciales={base ? aFormulario(base) : { nombre: '', criterios: [criterioVacio()] }}
            onGuardada={(creada) => creada && navegar(`/campanas/${campanaId}/rubricas/${creada.id}`, { replace: true })}
            onPublicada={recargar}
          />
        </Tarjeta>
      </>
    )
  }

  const rubrica = datos.find((r) => r.id === rubricaId)
  if (!rubrica) {
    return (
      <>
        <EncabezadoPagina titulo="Rúbrica" acciones={volver} />
        <EstadoError mensaje="La rúbrica no existe." />
      </>
    )
  }

  return (
    <>
      <EncabezadoPagina titulo={`${rubrica.nombre} · v${rubrica.version}`} acciones={volver} />
      <div className="mb-6">
        <BadgeEstadoRubrica estado={rubrica.estado} />
      </div>
      {rubrica.estado === 'Borrador' ? (
        <Tarjeta>
          {/* La clave cambia al guardar (el backend recrea los criterios): el formulario se reinicia con lo guardado. */}
          <RubricaEditorForm
            key={rubrica.criterios.map((c) => c.id).join(',') + rubrica.nombre}
            campanaId={campanaId}
            rubrica={rubrica}
            valoresIniciales={aFormulario(rubrica)}
            onGuardada={recargar}
            onPublicada={recargar}
          />
        </Tarjeta>
      ) : (
        <RubricaSoloLectura rubrica={rubrica} />
      )}
    </>
  )
}
