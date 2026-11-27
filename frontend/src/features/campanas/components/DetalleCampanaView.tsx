import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { Boton } from '../../../shared/components/Boton'
import { Dialogo } from '../../../shared/components/Dialogo'
import { EncabezadoPagina } from '../../../shared/components/EncabezadoPagina'
import { EstadoCarga, EstadoError } from '../../../shared/components/EstadosVista'
import { IconoEditar, IconoFlechaIzq } from '../../../shared/components/Iconos'
import { useConsulta } from '../../../shared/hooks/useConsulta'
import { obtenerCampana } from '../api/campanasApi'
import { CLASE_ENLACE_ACCION } from '../utils/estilos'
import { AsignacionesCard } from './AsignacionesCard'
import { BadgeEstadoCampana, BadgeRubricaActiva } from './BadgesCampana'
import { CampanaForm } from './CampanaForm'

/** Detalle de una campaña (AdminEmpresa): estado, edición y personas asignadas. */
export function DetalleCampanaView() {
  const { id = '' } = useParams()
  const [editando, setEditando] = useState(false)
  const { datos: campana, error, recargar } = useConsulta(`campana-${id}`, () => obtenerCampana(id))

  const volver = (
    <Link to="/campanas" className={CLASE_ENLACE_ACCION}>
      <IconoFlechaIzq tamano={18} />
      Volver a campañas
    </Link>
  )

  if (error) {
    return (
      <>
        <EncabezadoPagina titulo="Campaña" acciones={volver} />
        <EstadoError mensaje={error} onReintentar={recargar} />
      </>
    )
  }
  if (!campana) return <EstadoCarga texto="Cargando campaña…" />

  const acciones = (
    <>
      {volver}
      <Boton variante="secundario" icono={<IconoEditar tamano={18} />} onClick={() => setEditando(true)}>
        Editar
      </Boton>
    </>
  )

  return (
    <>
      <EncabezadoPagina titulo={campana.nombre} acciones={acciones} />
      <div className="mb-6 flex flex-wrap gap-2">
        <BadgeEstadoCampana activa={campana.activa} />
        <BadgeRubricaActiva rubricaActivaId={campana.rubricaActivaId} />
      </div>

      <AsignacionesCard campanaId={campana.id} />

      <Dialogo abierto={editando} titulo="Editar campaña" onCerrar={() => setEditando(false)}>
        {editando && (
          <CampanaForm
            campana={campana}
            onGuardada={() => {
              setEditando(false)
              recargar()
            }}
            onCancelar={() => setEditando(false)}
          />
        )}
      </Dialogo>
    </>
  )
}
