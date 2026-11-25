import { Badge } from '../../../shared/components/Badge'
import { Boton } from '../../../shared/components/Boton'
import { BadgeEstadoSuscripcion } from '../../../shared/components/estadosBadge'
import { IconoEditar } from '../../../shared/components/Iconos'
import { Tabla, type ColumnaTabla } from '../../../shared/components/Tabla'
import type { PlanDto } from '../../../shared/types/suscripciones.types'
import { formatearFecha, formatearNumero } from '../../../shared/utils/formato'
import type { EmpresaComercialDto } from '../types/plataforma.types'

interface PropsTablaEmpresas {
  empresas: EmpresaComercialDto[]
  planes: PlanDto[]
  onCambiar: (empresa: EmpresaComercialDto) => void
}

const SIN_DATO = <span className="text-texto-secundario">—</span>

export function TablaEmpresasPlataforma({ empresas, planes, onCambiar }: PropsTablaEmpresas) {
  const nombrePlan = (planId: string) => planes.find((p) => p.id === planId)?.nombre ?? 'Plan no disponible'

  const columnas: ColumnaTabla<EmpresaComercialDto>[] = [
    {
      clave: 'empresa',
      encabezado: 'Empresa',
      celda: (e) => (
        <div>
          <p className="font-medium">{e.nombre}</p>
          <p className="text-meta text-texto-secundario">{e.codigo}</p>
        </div>
      ),
    },
    {
      clave: 'estado',
      encabezado: 'Empresa activa',
      celda: (e) => (e.estado === 'Activa' ? <Badge tono="exito">Activa</Badge> : <Badge tono="error">Suspendida</Badge>),
    },
    { clave: 'plan', encabezado: 'Plan', celda: (e) => (e.suscripcion ? nombrePlan(e.suscripcion.planId) : SIN_DATO) },
    {
      clave: 'suscripcion',
      encabezado: 'Suscripción',
      celda: (e) => (e.suscripcion ? <BadgeEstadoSuscripcion estado={e.suscripcion.estado} /> : SIN_DATO),
    },
    {
      clave: 'vence',
      encabezado: 'Vence',
      ocultarEnMovil: true,
      celda: (e) => (e.suscripcion ? <span className="tabular-nums">{formatearFecha(e.suscripcion.fechaVencimiento)}</span> : SIN_DATO),
    },
    {
      clave: 'consumo',
      encabezado: 'Minutos facturables / contratados',
      alinear: 'derecha',
      ocultarEnMovil: true,
      celda: (e) =>
        e.consumo ? `${formatearNumero(e.consumo.minutosFacturables)} / ${formatearNumero(e.consumo.minutosContratados)}` : SIN_DATO,
    },
    {
      clave: 'acciones',
      encabezado: 'Acciones',
      alinear: 'derecha',
      celda: (e) => (
        <Boton variante="fantasma" tamano="sm" icono={<IconoEditar tamano={18} />} onClick={() => onCambiar(e)} aria-label={`Cambiar suscripción de ${e.nombre}`}>
          Cambiar
        </Boton>
      ),
    },
  ]

  return <Tabla descripcion="Empresas registradas" columnas={columnas} filas={empresas} claveFila={(e) => e.empresaId} />
}
