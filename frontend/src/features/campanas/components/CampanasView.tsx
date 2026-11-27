import { useState } from 'react'
import { Boton } from '../../../shared/components/Boton'
import { Dialogo } from '../../../shared/components/Dialogo'
import { EncabezadoPagina } from '../../../shared/components/EncabezadoPagina'
import { EstadoCarga, EstadoError, EstadoVacio } from '../../../shared/components/EstadosVista'
import { IconoMas } from '../../../shared/components/Iconos'
import { Paginador } from '../../../shared/components/Paginador'
import { Tarjeta } from '../../../shared/components/Tarjeta'
import { useConsulta } from '../../../shared/hooks/useConsulta'
import { listarCampanas } from '../api/campanasApi'
import type { CampanaDto } from '../types/campana.types'
import { CampanaForm } from './CampanaForm'
import { TablaCampanas } from './TablaCampanas'

const TAMANO_PAGINA = 10

/** Campañas de la empresa (AdminEmpresa): lista paginada, crear y editar. El detalle gestiona asignaciones y rúbricas. */
export function CampanasView() {
  const [pagina, setPagina] = useState(1)
  const [creando, setCreando] = useState(false)
  const [editando, setEditando] = useState<CampanaDto | null>(null)

  const { datos, error, cargando, recargar } = useConsulta(`campanas-${pagina}`, () =>
    listarCampanas({ pagina, tamanoPagina: TAMANO_PAGINA }),
  )

  const alTerminar = () => {
    setCreando(false)
    setEditando(null)
    recargar()
  }

  const botonCrear = (
    <Boton icono={<IconoMas tamano={18} />} onClick={() => setCreando(true)}>
      Nueva campaña
    </Boton>
  )

  return (
    <>
      <EncabezadoPagina
        titulo="Campañas"
        descripcion="Cada campaña agrupa a sus agentes y supervisores y se evalúa con su rúbrica activa."
        acciones={botonCrear}
      />

      <Tarjeta sinRelleno>
        {error && <EstadoError mensaje={error} onReintentar={recargar} />}
        {!error && !datos && cargando && <EstadoCarga texto="Cargando campañas…" />}
        {!error && datos && datos.total === 0 && (
          <EstadoVacio
            titulo="Aún no hay campañas"
            descripcion="Crea la primera para asignar a tu equipo y publicar su rúbrica."
            accion={botonCrear}
          />
        )}
        {!error && datos && datos.total > 0 && (
          <div aria-busy={cargando}>
            <TablaCampanas campanas={datos.items} onEditar={setEditando} />
            <Paginador
              pagina={datos.pagina}
              tamanoPagina={datos.tamanoPagina}
              total={datos.total}
              onCambiar={setPagina}
              deshabilitado={cargando}
            />
          </div>
        )}
      </Tarjeta>

      <Dialogo abierto={creando} titulo="Nueva campaña" onCerrar={() => setCreando(false)}>
        {creando && <CampanaForm onGuardada={alTerminar} onCancelar={() => setCreando(false)} />}
      </Dialogo>

      <Dialogo abierto={editando !== null} titulo="Editar campaña" onCerrar={() => setEditando(null)}>
        {editando && <CampanaForm campana={editando} onGuardada={alTerminar} onCancelar={() => setEditando(null)} />}
      </Dialogo>
    </>
  )
}
