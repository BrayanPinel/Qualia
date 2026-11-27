import { useState } from 'react'
import { Boton } from '../../../shared/components/Boton'
import { Dialogo } from '../../../shared/components/Dialogo'
import { EncabezadoPagina } from '../../../shared/components/EncabezadoPagina'
import { EstadoCarga, EstadoError, EstadoVacio } from '../../../shared/components/EstadosVista'
import { IconoMas } from '../../../shared/components/Iconos'
import { Paginador } from '../../../shared/components/Paginador'
import { Tarjeta } from '../../../shared/components/Tarjeta'
import { useConsulta } from '../../../shared/hooks/useConsulta'
import { useSesion } from '../../auth/context/useSesion'
import { listarLlamadas } from '../api/llamadasApi'
import { useCargaManual } from '../hooks/useCargaManual'
import { useCatalogosLlamadas } from '../hooks/useCatalogosLlamadas'
import { SIN_FILTROS, type SeleccionFiltros } from '../types/llamada.types'
import { CargarLlamadaForm } from './CargarLlamadaForm'
import { FiltrosLlamadas } from './FiltrosLlamadas'
import { TablaLlamadas } from './TablaLlamadas'

const TAMANO_PAGINA = 10

interface PropsLlamadasView {
  /** true: bandeja del Supervisor con las evaluaciones que la IA marcó para revisión (requiereRevision=true). */
  bandejaRevision?: boolean
}

/** Lista paginada de llamadas con filtros. El backend decide qué llamadas ve cada rol. */
export function LlamadasView({ bandejaRevision = false }: PropsLlamadasView) {
  const { sesion } = useSesion()
  const rol = sesion?.rol ?? 'Agente'
  const [pagina, setPagina] = useState(1)
  const [filtros, setFiltros] = useState<SeleccionFiltros>(SIN_FILTROS)
  const [cargando, setCargando] = useState(false)
  const carga = useCargaManual(rol)
  const catalogos = useCatalogosLlamadas(rol)

  const { datos, error, cargando: consultando, recargar } = useConsulta(
    `llamadas-${bandejaRevision}-${pagina}-${filtros.campanaId}-${filtros.agenteId}-${filtros.estado}`,
    () =>
      listarLlamadas({
        pagina,
        tamanoPagina: TAMANO_PAGINA,
        campanaId: filtros.campanaId || undefined,
        agenteId: filtros.agenteId || undefined,
        estado: bandejaRevision ? undefined : filtros.estado || undefined,
        requiereRevision: bandejaRevision ? true : undefined,
      }),
  )

  const cambiarFiltros = (valor: SeleccionFiltros) => {
    setFiltros(valor)
    setPagina(1)
  }

  const alCargar = () => {
    setCargando(false)
    recargar()
  }

  const alDeshabilitar = (mensaje: string) => {
    setCargando(false)
    carga.deshabilitar(mensaje)
  }

  const titulo = bandejaRevision ? 'Revisión de calidad' : rol === 'Agente' ? 'Mis llamadas' : 'Llamadas'
  const descripcion = bandejaRevision
    ? 'Llamadas cuya evaluación de la IA requiere revisión humana. Confírmala o ajústala.'
    : 'Estado del procesamiento, transcripción y evaluación de cada llamada.'

  const botonCargar = !bandejaRevision && carga.visible && (
    <Boton icono={<IconoMas tamano={18} />} onClick={() => setCargando(true)}>
      Cargar llamada
    </Boton>
  )

  return (
    <>
      <EncabezadoPagina titulo={titulo} descripcion={descripcion} acciones={botonCargar || undefined} />

      {carga.aviso && !bandejaRevision && (
        <p role="status" className="mb-4 rounded-lg border border-borde bg-superficie px-4 py-3 text-sm text-texto-secundario">
          {carga.aviso}
        </p>
      )}

      <Tarjeta sinRelleno>
        <FiltrosLlamadas
          valor={filtros}
          campanas={catalogos.campanas}
          agentes={catalogos.agentes}
          ocultarEstado={bandejaRevision}
          onCambiar={cambiarFiltros}
        />
        {error && <EstadoError mensaje={error} onReintentar={recargar} />}
        {!error && !datos && consultando && <EstadoCarga texto="Cargando llamadas…" />}
        {!error && datos && datos.total === 0 && (
          <EstadoVacio
            titulo={bandejaRevision ? 'No hay evaluaciones por revisar' : 'No hay llamadas'}
            descripcion={
              bandejaRevision
                ? 'Cuando la IA detecte fallas en una evaluación aparecerá aquí.'
                : 'Prueba con otros filtros o espera a que lleguen nuevas grabaciones.'
            }
          />
        )}
        {!error && datos && datos.total > 0 && (
          <div aria-busy={consultando}>
            <TablaLlamadas
              llamadas={datos.items}
              nombreCampana={catalogos.nombreCampana}
              nombreAgente={catalogos.nombreAgente}
              bandejaRevision={bandejaRevision}
            />
            <Paginador
              pagina={datos.pagina}
              tamanoPagina={datos.tamanoPagina}
              total={datos.total}
              onCambiar={setPagina}
              deshabilitado={consultando}
            />
          </div>
        )}
      </Tarjeta>

      <Dialogo
        abierto={cargando}
        titulo="Cargar llamada"
        descripcion="La duración se mide en el servidor y la llamada queda disponible para evaluar."
        onCerrar={() => setCargando(false)}
      >
        <CargarLlamadaForm
          rol={rol}
          campanas={catalogos.campanas.filter((c) => c.activa && c.rubricaActivaId)}
          onCargada={alCargar}
          onCancelar={() => setCargando(false)}
          onDeshabilitada={alDeshabilitar}
        />
      </Dialogo>
    </>
  )
}
