import { useState } from 'react'
import { extraerMensajeErrorBackend } from '../../../shared/api/extraerMensajeErrorBackend'
import { Boton } from '../../../shared/components/Boton'
import { Dialogo } from '../../../shared/components/Dialogo'
import { EstadoCarga, EstadoError, EstadoVacio } from '../../../shared/components/EstadosVista'
import { AlertaFormulario } from '../../../shared/components/Formulario'
import { Tarjeta } from '../../../shared/components/Tarjeta'
import { useConsulta } from '../../../shared/hooks/useConsulta'
import type { UsuarioDto } from '../../../shared/types/comunes.types'
import { listarAsignados, quitarUsuario } from '../api/campanasApi'
import { AsignarUsuarioForm } from './AsignarUsuarioForm'
import { TablaAsignados } from './TablaAsignados'

/** Agentes y supervisores de la campaña: asignar y quitar. Un supervisor solo revisa las campañas asignadas. */
export function AsignacionesCard({ campanaId }: { campanaId: string }) {
  const [quitando, setQuitando] = useState<UsuarioDto | null>(null)
  const [enviando, setEnviando] = useState(false)
  const [errorQuitar, setErrorQuitar] = useState<string | null>(null)
  const { datos, error, cargando, recargar } = useConsulta(`asignados-${campanaId}`, () => listarAsignados(campanaId))

  const cerrarQuitar = () => {
    setQuitando(null)
    setErrorQuitar(null)
  }

  const confirmarQuitar = async () => {
    if (!quitando) return
    setEnviando(true)
    setErrorQuitar(null)
    try {
      await quitarUsuario(campanaId, quitando.id)
      cerrarQuitar()
      recargar()
    } catch (e) {
      setErrorQuitar(extraerMensajeErrorBackend(e))
    } finally {
      setEnviando(false)
    }
  }

  return (
    <Tarjeta titulo="Personas asignadas" descripcion="Solo se asignan agentes y supervisores activos." sinRelleno>
      <div className="border-b border-borde px-6 py-5">
        <AsignarUsuarioForm campanaId={campanaId} asignados={datos ?? []} onAsignado={recargar} />
      </div>

      {error && <EstadoError mensaje={error} onReintentar={recargar} />}
      {!error && !datos && cargando && <EstadoCarga texto="Cargando asignaciones…" />}
      {!error && datos && datos.length === 0 && (
        <EstadoVacio titulo="Nadie asignado todavía" descripcion="Asigna agentes y al menos un supervisor para revisar sus llamadas." />
      )}
      {!error && datos && datos.length > 0 && (
        <div aria-busy={cargando}>
          <TablaAsignados usuarios={datos} onQuitar={setQuitando} />
        </div>
      )}

      <Dialogo
        abierto={quitando !== null}
        titulo="Quitar de la campaña"
        descripcion={quitando ? `${quitando.nombre} dejará de ver las llamadas de esta campaña.` : undefined}
        onCerrar={cerrarQuitar}
      >
        <div className="flex flex-col gap-5">
          <AlertaFormulario mensaje={errorQuitar} />
          <div className="flex justify-end gap-2">
            <Boton variante="secundario" onClick={cerrarQuitar}>
              Cancelar
            </Boton>
            <Boton variante="peligro" cargando={enviando} onClick={confirmarQuitar}>
              Quitar a {quitando?.nombre}
            </Boton>
          </div>
        </div>
      </Dialogo>
    </Tarjeta>
  )
}
