import { Link } from 'react-router-dom'
import { BadgeEstadoDisputa } from '../../../shared/components/estadosBadge'
import { IconoFlechaDer } from '../../../shared/components/Iconos'
import { Tabla, type ColumnaTabla } from '../../../shared/components/Tabla'
import type { Rol } from '../../../shared/types/comunes.types'
import { formatearFecha } from '../../../shared/utils/formato'
import { CLASE_ENLACE_ACCION } from '../../llamadas/utils/estilos'
import { idCorto } from '../../llamadas/utils/tiempo'
import type { DisputaDto } from '../types/disputa.types'

interface PropsTablaDisputas {
  disputas: DisputaDto[]
  rol: Rol
}

export function TablaDisputas({ disputas, rol }: PropsTablaDisputas) {
  const columnas: ColumnaTabla<DisputaDto>[] = [
    { clave: 'fecha', encabezado: 'Abierta el', celda: (d) => <span className="whitespace-nowrap">{formatearFecha(d.creadaEn)}</span> },
    { clave: 'estado', encabezado: 'Estado', celda: (d) => <BadgeEstadoDisputa estado={d.estado} /> },
    { clave: 'motivo', encabezado: 'Motivo', celda: (d) => <span className="line-clamp-2 max-w-md text-texto-secundario">{d.motivo}</span>, ocultarEnMovil: true },
    {
      clave: 'llamada',
      encabezado: 'Llamada',
      celda: (d) => (
        <Link to={`/llamadas/${d.llamadaId}`} className="tabular-nums hover:underline">
          {idCorto(d.llamadaId)}
        </Link>
      ),
    },
    {
      clave: 'acciones',
      encabezado: 'Acciones',
      alinear: 'derecha',
      celda: (d) => (
        <Link to={`/disputas/${d.id}`} className={CLASE_ENLACE_ACCION}>
          {rol === 'Supervisor' && d.estado === 'Abierta' ? 'Resolver' : 'Ver'}
          <IconoFlechaDer tamano={18} />
        </Link>
      ),
    },
  ]

  return <Tabla descripcion="Disputas" columnas={columnas} filas={disputas} claveFila={(d) => d.id} />
}
