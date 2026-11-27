import { Badge } from '../../../shared/components/Badge'
import { Boton } from '../../../shared/components/Boton'
import { Tabla, type ColumnaTabla } from '../../../shared/components/Tabla'
import { formatearNumero } from '../../../shared/utils/formato'
import type { ScorecardDto } from '../types/desempeno.types'

interface PropsTablaScorecards {
  scorecards: ScorecardDto[]
  nombreAgente: (id: string) => string
  onVer: (scorecard: ScorecardDto) => void
}

export function TablaScorecards({ scorecards, nombreAgente, onVer }: PropsTablaScorecards) {
  const columnas: ColumnaTabla<ScorecardDto>[] = [
    { clave: 'agente', encabezado: 'Agente', celda: (s) => <span className="font-medium">{nombreAgente(s.agenteId)}</span> },
    { clave: 'elegibles', encabezado: 'Elegibles', alinear: 'derecha', celda: (s) => s.llamadasElegibles },
    { clave: 'evaluadas', encabezado: 'Evaluadas', alinear: 'derecha', celda: (s) => s.llamadasEvaluadas, ocultarEnMovil: true },
    {
      clave: 'promedio',
      encabezado: 'Promedio',
      alinear: 'derecha',
      celda: (s) =>
        s.puntajePromedio === null ? (
          <Badge tono="neutro" sinIcono>
            Muestra insuficiente
          </Badge>
        ) : (
          formatearNumero(s.puntajePromedio)
        ),
    },
    { clave: 'criticos', encabezado: 'Críticos fallidos', alinear: 'derecha', celda: (s) => s.criticosFallidos, ocultarEnMovil: true },
    {
      clave: 'acciones',
      encabezado: 'Acciones',
      alinear: 'derecha',
      celda: (s) => (
        <Boton variante="fantasma" tamano="sm" onClick={() => onVer(s)} aria-label={`Ver scorecard de ${nombreAgente(s.agenteId)}`}>
          Ver scorecard
        </Boton>
      ),
    },
  ]

  return <Tabla descripcion="Scorecards de los agentes" columnas={columnas} filas={scorecards} claveFila={(s) => s.agenteId} />
}
