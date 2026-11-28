import { Link } from 'react-router-dom'
import { Badge } from '../../../shared/components/Badge'
import { IconoDocumento } from '../../../shared/components/Iconos'
import { Tabla, type ColumnaTabla } from '../../../shared/components/Tabla'
import { Tarjeta } from '../../../shared/components/Tarjeta'
import { formatearNumero } from '../../../shared/utils/formato'
import { rutaNuevaVersion, type CriterioDto, type RubricaDto } from '../types/rubrica.types'
import { CLASE_ENLACE_ACCION } from '../utils/estilos'

/** Versión publicada (Activa o Retirada): inmutable, sin inputs. Para cambiarla se crea una versión nueva. */
export function RubricaSoloLectura({ rubrica }: { rubrica: RubricaDto }) {
  const columnas: ColumnaTabla<CriterioDto>[] = [
    {
      clave: 'nombre',
      encabezado: 'Criterio',
      celda: (c) => (
        <div className="flex flex-col gap-1 py-1">
          <span className="flex flex-wrap items-center gap-2 font-medium">
            {c.nombre}
            {c.esCritico && <Badge tono="error" sinIcono>Crítico</Badge>}
          </span>
          <span className="text-texto-secundario">{c.descripcion}</span>
        </div>
      ),
    },
    { clave: 'peso', encabezado: 'Peso', alinear: 'derecha', celda: (c) => <span className="tabular-nums">{formatearNumero(c.peso)}</span> },
    { clave: 'umbral', encabezado: 'Umbral', alinear: 'derecha', celda: (c) => <span className="tabular-nums">{formatearNumero(c.umbralCumplimiento)}</span> },
  ]

  // Restaurar o ajustar una versión publicada = copiar sus criterios en una versión nueva (las publicadas no cambian).
  const usarComoBase = (
    <Link to={rutaNuevaVersion(rubrica.campanaId, rubrica.id)} className={`${CLASE_ENLACE_ACCION} border border-borde`}>
      <IconoDocumento tamano={18} />
      Usar como base
    </Link>
  )

  return (
    <Tarjeta
      titulo={`${rubrica.criterios.length} criterios`}
      descripcion={
        rubrica.estado === 'Retirada'
          ? 'Versión retirada: no se puede editar ni reactivar. Para volver a usarla, créala como versión nueva y publícala.'
          : 'Esta versión ya fue publicada y no se puede editar. Para cambiarla, úsala como base de una versión nueva.'
      }
      acciones={usarComoBase}
      sinRelleno
    >
      <Tabla descripcion={`Criterios de la versión ${rubrica.version}`} columnas={columnas} filas={rubrica.criterios} claveFila={(c) => c.id} />
    </Tarjeta>
  )
}
