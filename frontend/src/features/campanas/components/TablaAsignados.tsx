import { Boton } from '../../../shared/components/Boton'
import { BadgeActivo, BadgeRol } from '../../../shared/components/estadosBadge'
import { IconoX } from '../../../shared/components/Iconos'
import { Tabla, type ColumnaTabla } from '../../../shared/components/Tabla'
import type { UsuarioDto } from '../../../shared/types/comunes.types'

interface PropsTablaAsignados {
  usuarios: UsuarioDto[]
  onQuitar: (usuario: UsuarioDto) => void
}

export function TablaAsignados({ usuarios, onQuitar }: PropsTablaAsignados) {
  const columnas: ColumnaTabla<UsuarioDto>[] = [
    { clave: 'nombre', encabezado: 'Nombre', celda: (u) => <span className="font-medium">{u.nombre}</span> },
    { clave: 'email', encabezado: 'Email', celda: (u) => <span className="text-texto-secundario">{u.email}</span>, ocultarEnMovil: true },
    { clave: 'rol', encabezado: 'Rol', celda: (u) => <BadgeRol rol={u.rol} /> },
    { clave: 'estado', encabezado: 'Estado', celda: (u) => <BadgeActivo activo={u.activo} /> },
    {
      clave: 'acciones',
      encabezado: 'Acciones',
      alinear: 'derecha',
      celda: (u) => (
        <Boton variante="fantasma" tamano="sm" icono={<IconoX tamano={18} />} onClick={() => onQuitar(u)} aria-label={`Quitar a ${u.nombre}`}>
          Quitar
        </Boton>
      ),
    },
  ]

  return <Tabla descripcion="Personas asignadas a la campaña" columnas={columnas} filas={usuarios} claveFila={(u) => u.id} />
}
