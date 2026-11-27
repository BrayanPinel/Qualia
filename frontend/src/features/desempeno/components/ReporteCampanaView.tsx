import { Badge } from '../../../shared/components/Badge'
import { EncabezadoPagina } from '../../../shared/components/EncabezadoPagina'
import { EstadoCarga, EstadoError } from '../../../shared/components/EstadosVista'
import { Tabla, type ColumnaTabla } from '../../../shared/components/Tabla'
import { Tarjeta } from '../../../shared/components/Tarjeta'
import { useConsulta } from '../../../shared/hooks/useConsulta'
import { formatearNumero } from '../../../shared/utils/formato'
import { obtenerReporteCampana } from '../api/desempenoApi'
import type { FiltroDesempeno, ReporteCampanaDto } from '../types/desempeno.types'
import { Indicador } from './Indicador'
import { PanelFiltroDesempeno } from './PanelFiltroDesempeno'

interface FilaReporte {
  indicador: string
  valor: string
  nota: string
}

const COLUMNAS: ColumnaTabla<FilaReporte>[] = [
  { clave: 'indicador', encabezado: 'Indicador', celda: (f) => <span className="font-medium">{f.indicador}</span> },
  { clave: 'valor', encabezado: 'Valor', alinear: 'derecha', celda: (f) => f.valor },
  { clave: 'nota', encabezado: 'Cómo se calcula', celda: (f) => <span className="text-texto-secundario">{f.nota}</span>, ocultarEnMovil: true },
]

function filas(r: ReporteCampanaDto): FilaReporte[] {
  return [
    { indicador: 'Periodo', valor: r.periodo, nota: 'Mes UTC según la fecha de la llamada.' },
    { indicador: 'Llamadas elegibles', valor: String(r.llamadasElegibles), nota: 'Evaluaciones oficiales sin disputa abierta.' },
    { indicador: 'Muestra suficiente', valor: r.muestraSuficiente ? 'Sí' : 'No', nota: 'Alcanza el mínimo de muestra de la empresa.' },
    { indicador: 'Promedio', valor: r.promedio === null ? '—' : formatearNumero(r.promedio), nota: 'Media de los totales por llamada.' },
    { indicador: 'Críticos fallidos', valor: String(r.criticosFallidos), nota: 'Criterios críticos bajo su umbral.' },
  ]
}

function Reporte({ filtro, campana }: { filtro: FiltroDesempeno; campana: string }) {
  const { datos, error, recargar } = useConsulta(`reporte-${filtro.campanaId}-${filtro.periodo}`, () =>
    obtenerReporteCampana(filtro),
  )

  if (error) return <Tarjeta><EstadoError mensaje={error} onReintentar={recargar} /></Tarjeta>
  if (!datos) return <EstadoCarga texto="Calculando el reporte…" />

  return (
    <>
      <div className="grid gap-4 sm:grid-cols-3">
        <Indicador
          etiqueta="Promedio de la campaña"
          valor={datos.promedio === null ? '—' : formatearNumero(datos.promedio)}
          contexto={datos.muestraSuficiente ? undefined : <Badge tono="neutro" sinIcono>Muestra insuficiente</Badge>}
        />
        <Indicador etiqueta="Llamadas elegibles" valor={datos.llamadasElegibles} />
        <Indicador etiqueta="Críticos fallidos" valor={datos.criticosFallidos} />
      </div>
      <Tarjeta titulo={campana} descripcion={`Detalle del periodo ${datos.periodo}`} sinRelleno>
        <Tabla descripcion="Indicadores del reporte" columnas={COLUMNAS} filas={filas(datos)} claveFila={(f) => f.indicador} />
      </Tarjeta>
    </>
  )
}

/** Reporte agregado de una campaña por mes (AdminEmpresa, Supervisor asignado y Auditor). */
export function ReporteCampanaView() {
  return (
    <>
      <EncabezadoPagina titulo="Reportes" descripcion="Indicadores agregados de la campaña en el periodo elegido." />
      <PanelFiltroDesempeno>{(filtro, campana) => <Reporte filtro={filtro} campana={campana} />}</PanelFiltroDesempeno>
    </>
  )
}
