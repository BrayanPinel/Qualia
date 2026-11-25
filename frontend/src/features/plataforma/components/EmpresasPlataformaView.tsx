import { useState } from 'react'
import { obtenerPlanes } from '../../../shared/api/planesApi'
import { Dialogo } from '../../../shared/components/Dialogo'
import { EncabezadoPagina } from '../../../shared/components/EncabezadoPagina'
import { EstadoCarga, EstadoError, EstadoVacio } from '../../../shared/components/EstadosVista'
import { Paginador } from '../../../shared/components/Paginador'
import { Tarjeta } from '../../../shared/components/Tarjeta'
import { useConsulta } from '../../../shared/hooks/useConsulta'
import { listarEmpresas } from '../api/plataformaApi'
import type { EmpresaComercialDto } from '../types/plataforma.types'
import { CambiarSuscripcionForm } from './CambiarSuscripcionForm'
import { TablaEmpresasPlataforma } from './TablaEmpresasPlataforma'

const TAMANO_PAGINA = 20

/** PlatformAdmin: empresas con su plan, estado y consumo del mes; cambio de suscripción. */
export function EmpresasPlataformaView() {
  const [pagina, setPagina] = useState(1)
  const [seleccionada, setSeleccionada] = useState<EmpresaComercialDto | null>(null)

  const empresas = useConsulta(`empresas-${pagina}`, () => listarEmpresas({ pagina, tamanoPagina: TAMANO_PAGINA }))
  const planes = useConsulta('planes', obtenerPlanes)

  const error = empresas.error ?? planes.error
  const datos = empresas.datos
  const listos = datos && planes.datos

  const recargarTodo = () => {
    empresas.recargar()
    planes.recargar()
  }

  return (
    <>
      <EncabezadoPagina
        titulo="Empresas"
        descripcion="Plan, estado de la suscripción y consumo del mes de cada empresa. Sin acceso al contenido de las llamadas."
      />

      <Tarjeta sinRelleno>
        {error && <EstadoError mensaje={error} onReintentar={recargarTodo} />}
        {!error && !listos && <EstadoCarga texto="Cargando empresas…" />}
        {!error && listos && datos.total === 0 && <EstadoVacio titulo="Aún no hay empresas registradas" />}
        {!error && listos && datos.total > 0 && (
          <div aria-busy={empresas.cargando}>
            <TablaEmpresasPlataforma empresas={datos.items} planes={planes.datos ?? []} onCambiar={setSeleccionada} />
            <Paginador
              pagina={datos.pagina}
              tamanoPagina={datos.tamanoPagina}
              total={datos.total}
              onCambiar={setPagina}
              deshabilitado={empresas.cargando}
            />
          </div>
        )}
      </Tarjeta>

      <Dialogo
        abierto={seleccionada !== null}
        titulo="Cambiar suscripción"
        descripcion={seleccionada ? `${seleccionada.nombre} (${seleccionada.codigo})` : undefined}
        onCerrar={() => setSeleccionada(null)}
      >
        {seleccionada && (
          <CambiarSuscripcionForm
            empresa={seleccionada}
            planes={planes.datos ?? []}
            onGuardado={() => {
              setSeleccionada(null)
              empresas.recargar()
            }}
            onCancelar={() => setSeleccionada(null)}
          />
        )}
      </Dialogo>
    </>
  )
}
