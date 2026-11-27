import { Link } from 'react-router-dom'
import { Boton } from '../../../shared/components/Boton'
import { IconoEditar, IconoFlechaDer } from '../../../shared/components/Iconos'
import { Tabla, type ColumnaTabla } from '../../../shared/components/Tabla'
import type { CampanaDto } from '../types/campana.types'
import { CLASE_ENLACE_ACCION } from '../utils/estilos'
import { BadgeEstadoCampana, BadgeRubricaActiva } from './BadgesCampana'

interface PropsTablaCampanas {
  campanas: CampanaDto[]
  onEditar: (campana: CampanaDto) => void
}

export function TablaCampanas({ campanas, onEditar }: PropsTablaCampanas) {
  const columnas: ColumnaTabla<CampanaDto>[] = [
    {
      clave: 'nombre',
      encabezado: 'Nombre',
      celda: (c) => (
        <Link to={`/campanas/${c.id}`} className="font-medium hover:underline">
          {c.nombre}
        </Link>
      ),
    },
    { clave: 'estado', encabezado: 'Estado', celda: (c) => <BadgeEstadoCampana activa={c.activa} /> },
    { clave: 'rubrica', encabezado: 'Rúbrica', celda: (c) => <BadgeRubricaActiva rubricaActivaId={c.rubricaActivaId} />, ocultarEnMovil: true },
    {
      clave: 'acciones',
      encabezado: 'Acciones',
      alinear: 'derecha',
      celda: (c) => (
        <div className="flex justify-end gap-1">
          <Boton variante="fantasma" tamano="sm" icono={<IconoEditar tamano={18} />} onClick={() => onEditar(c)} aria-label={`Editar ${c.nombre}`}>
            Editar
          </Boton>
          <Link to={`/campanas/${c.id}`} className={CLASE_ENLACE_ACCION} aria-label={`Abrir ${c.nombre}`}>
            Abrir
            <IconoFlechaDer tamano={18} />
          </Link>
        </div>
      ),
    },
  ]

  return <Tabla descripcion="Campañas de la empresa" columnas={columnas} filas={campanas} claveFila={(c) => c.id} />
}
