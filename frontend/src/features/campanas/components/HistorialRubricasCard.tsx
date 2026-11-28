import { Link } from 'react-router-dom'
import { EstadoCarga, EstadoError, EstadoVacio } from '../../../shared/components/EstadosVista'
import { IconoFlechaDer, IconoMas } from '../../../shared/components/Iconos'
import { Tabla, type ColumnaTabla } from '../../../shared/components/Tabla'
import { Tarjeta } from '../../../shared/components/Tarjeta'
import { useConsulta } from '../../../shared/hooks/useConsulta'
import { listarRubricas } from '../api/rubricasApi'
import { rutaNuevaVersion, type RubricaDto } from '../types/rubrica.types'
import { CLASE_ENLACE_ACCION } from '../utils/estilos'
import { BadgeEstadoRubrica } from './BadgesCampana'

/** Versiones de la rúbrica de la campaña. Solo un Borrador se edita; las publicadas se consultan en solo lectura. */
export function HistorialRubricasCard({ campanaId }: { campanaId: string }) {
  const { datos, error, cargando, recargar } = useConsulta(`rubricas-${campanaId}`, () => listarRubricas(campanaId))

  const nuevaVersion = (
    <Link to={rutaNuevaVersion(campanaId)} className={`${CLASE_ENLACE_ACCION} border border-borde`}>
      <IconoMas tamano={18} />
      Nueva versión
    </Link>
  )

  const columnas: ColumnaTabla<RubricaDto>[] = [
    { clave: 'version', encabezado: 'Versión', celda: (r) => <span className="font-medium tabular-nums">v{r.version}</span> },
    { clave: 'nombre', encabezado: 'Nombre', celda: (r) => r.nombre },
    { clave: 'estado', encabezado: 'Estado', celda: (r) => <BadgeEstadoRubrica estado={r.estado} /> },
    { clave: 'criterios', encabezado: 'Criterios', alinear: 'derecha', celda: (r) => <span className="tabular-nums">{r.criterios.length}</span>, ocultarEnMovil: true },
    {
      clave: 'acciones',
      encabezado: 'Acciones',
      alinear: 'derecha',
      celda: (r) => (
        <Link to={`/campanas/${campanaId}/rubricas/${r.id}`} className={CLASE_ENLACE_ACCION} aria-label={`Abrir versión ${r.version}`}>
          {r.estado === 'Borrador' ? 'Editar' : 'Ver'}
          <IconoFlechaDer tamano={18} />
        </Link>
      ),
    },
  ]

  return (
    <Tarjeta titulo="Rúbricas" descripcion="Publicar una versión retira la activa anterior." acciones={nuevaVersion} sinRelleno>
      {error && <EstadoError mensaje={error} onReintentar={recargar} />}
      {!error && !datos && cargando && <EstadoCarga texto="Cargando rúbricas…" />}
      {!error && datos && datos.length === 0 && (
        <EstadoVacio titulo="Sin rúbricas todavía" descripcion="Crea la primera versión y publícala para evaluar las llamadas." accion={nuevaVersion} />
      )}
      {!error && datos && datos.length > 0 && (
        <Tabla descripcion="Versiones de la rúbrica" columnas={columnas} filas={datos} claveFila={(r) => r.id} />
      )}
    </Tarjeta>
  )
}
