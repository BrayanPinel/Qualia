import { useState } from 'react'
import { Boton } from '../../../shared/components/Boton'
import { Dialogo } from '../../../shared/components/Dialogo'
import { EncabezadoPagina } from '../../../shared/components/EncabezadoPagina'
import { EstadoCarga, EstadoError, EstadoVacio } from '../../../shared/components/EstadosVista'
import { IconoMas } from '../../../shared/components/Iconos'
import { Paginador } from '../../../shared/components/Paginador'
import { Tarjeta } from '../../../shared/components/Tarjeta'
import { useConsulta } from '../../../shared/hooks/useConsulta'
import type { UsuarioDto } from '../../../shared/types/comunes.types'
import { listarUsuarios } from '../api/usuariosApi'
import { CrearUsuarioForm } from './CrearUsuarioForm'
import { EditarUsuarioForm } from './EditarUsuarioForm'
import { TablaUsuarios } from './TablaUsuarios'

const TAMANO_PAGINA = 10

/** Usuarios de la empresa (AdminEmpresa): lista paginada, crear y editar. */
export function UsuariosView() {
  const [pagina, setPagina] = useState(1)
  const [creando, setCreando] = useState(false)
  const [editando, setEditando] = useState<UsuarioDto | null>(null)

  const { datos, error, cargando, recargar } = useConsulta(`usuarios-${pagina}`, () =>
    listarUsuarios({ pagina, tamanoPagina: TAMANO_PAGINA }),
  )

  const alTerminar = () => {
    setCreando(false)
    setEditando(null)
    recargar()
  }

  const botonCrear = (
    <Boton icono={<IconoMas tamano={18} />} onClick={() => setCreando(true)}>
      Crear usuario
    </Boton>
  )

  return (
    <>
      <EncabezadoPagina
        titulo="Usuarios"
        descripcion="Cuentas de tu empresa. El rol define qué puede ver y hacer cada persona."
        acciones={botonCrear}
      />

      <Tarjeta sinRelleno>
        {error && <EstadoError mensaje={error} onReintentar={recargar} />}
        {!error && !datos && cargando && <EstadoCarga texto="Cargando usuarios…" />}
        {!error && datos && datos.total === 0 && (
          <EstadoVacio titulo="Aún no hay usuarios" descripcion="Crea la primera cuenta para tu equipo." />
        )}
        {!error && datos && datos.total > 0 && (
          <div aria-busy={cargando}>
            <TablaUsuarios usuarios={datos.items} onEditar={setEditando} />
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

      <Dialogo abierto={creando} titulo="Crear usuario" onCerrar={() => setCreando(false)}>
        <CrearUsuarioForm onCreado={alTerminar} onCancelar={() => setCreando(false)} />
      </Dialogo>

      <Dialogo abierto={editando !== null} titulo="Editar usuario" onCerrar={() => setEditando(null)}>
        {editando && <EditarUsuarioForm usuario={editando} onGuardado={alTerminar} onCancelar={() => setEditando(null)} />}
      </Dialogo>
    </>
  )
}
