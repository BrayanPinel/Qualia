import { useState } from 'react'
import { EncabezadoPagina } from '../../../shared/components/EncabezadoPagina'
import { EstadoCarga, EstadoError, EstadoVacio } from '../../../shared/components/EstadosVista'
import { CampoSelector } from '../../../shared/components/Formulario'
import { Paginador } from '../../../shared/components/Paginador'
import { Tarjeta } from '../../../shared/components/Tarjeta'
import { useConsulta } from '../../../shared/hooks/useConsulta'
import type { EstadoDisputa } from '../../../shared/types/comunes.types'
import { useSesion } from '../../auth/context/useSesion'
import { listarDisputas } from '../api/disputasApi'
import { ESTADOS_DISPUTA } from '../types/disputa.types'
import { TablaDisputas } from './TablaDisputas'

const TAMANO_PAGINA = 10

const DESCRIPCION: Record<string, string> = {
  Supervisor: 'Disputas de las campañas que supervisas. Ábrelas para mantener o modificar las notas.',
  Agente: 'Tus disputas y su resolución.',
}

/** Disputas visibles según el rol: Admin todas (solo lectura), Supervisor las de sus campañas, Agente las suyas. */
export function BandejaDisputasView() {
  const { sesion } = useSesion()
  const rol = sesion?.rol ?? 'Agente'
  const [pagina, setPagina] = useState(1)
  // Por defecto, las abiertas: es lo que requiere acción.
  const [estado, setEstado] = useState<EstadoDisputa | ''>('Abierta')

  const { datos, error, cargando, recargar } = useConsulta(`disputas-${estado}-${pagina}`, () =>
    listarDisputas({ pagina, tamanoPagina: TAMANO_PAGINA, estado: estado || undefined }),
  )

  return (
    <>
      <EncabezadoPagina titulo={rol === 'Agente' ? 'Mis disputas' : 'Disputas'} descripcion={DESCRIPCION[rol] ?? 'Disputas de la empresa y su resolución.'} />
      <Tarjeta sinRelleno>
        <div className="px-6 pt-6 pb-4 sm:max-w-xs">
          <CampoSelector
            etiqueta="Estado"
            value={estado}
            onChange={(e) => {
              setEstado(e.target.value as EstadoDisputa | '')
              setPagina(1)
            }}
          >
            <option value="">Todas</option>
            {ESTADOS_DISPUTA.map((e) => (
              <option key={e} value={e}>
                {e === 'Abierta' ? 'Abiertas' : 'Resueltas'}
              </option>
            ))}
          </CampoSelector>
        </div>
        {error && <EstadoError mensaje={error} onReintentar={recargar} />}
        {!error && !datos && cargando && <EstadoCarga texto="Cargando disputas…" />}
        {!error && datos && datos.total === 0 && (
          <EstadoVacio titulo={estado === 'Abierta' ? 'No hay disputas abiertas' : 'No hay disputas'} descripcion="Cambia el filtro de estado para ver otras." />
        )}
        {!error && datos && datos.total > 0 && (
          <div aria-busy={cargando}>
            <TablaDisputas disputas={datos.items} rol={rol} />
            <Paginador pagina={datos.pagina} tamanoPagina={datos.tamanoPagina} total={datos.total} onCambiar={setPagina} deshabilitado={cargando} />
          </div>
        )}
      </Tarjeta>
    </>
  )
}
