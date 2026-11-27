import { EstadoVacio } from '../../../shared/components/EstadosVista'
import { Tabla, type ColumnaTabla } from '../../../shared/components/Tabla'
import { formatearNumero } from '../../../shared/utils/formato'
import type { PromedioCriterioDto, ScorecardDto } from '../types/desempeno.types'
import { Indicador } from './Indicador'

const texto = (valor: number | null) => (valor === null ? '—' : formatearNumero(valor))

function textoTendencia(tendencia: number | null) {
  if (tendencia === null) return '—'
  return `${tendencia > 0 ? '+' : ''}${formatearNumero(tendencia)}`
}

const COLUMNAS: ColumnaTabla<PromedioCriterioDto>[] = [
  { clave: 'nombre', encabezado: 'Criterio', celda: (c) => <span className="font-medium">{c.nombre}</span> },
  { clave: 'promedio', encabezado: 'Promedio', alinear: 'derecha', celda: (c) => <span className="font-medium">{formatearNumero(c.promedio)}</span> },
  { clave: 'cantidad', encabezado: 'Llamadas', alinear: 'derecha', celda: (c) => c.cantidad },
  { clave: 'version', encabezado: 'Rúbrica', celda: (c) => `v${c.version}`, ocultarEnMovil: true },
]

/** Promedio general, tendencia, comparación con la campaña y promedio por criterio de un scorecard. */
export function ScorecardDetalle({ scorecard }: { scorecard: ScorecardDto }) {
  return (
    // Consulta de contenedor: se ve igual en la página y dentro del diálogo angosto.
    <div className="@container flex flex-col gap-6">
      <div className="grid grid-cols-2 gap-4 @3xl:grid-cols-4">
        <Indicador
          etiqueta="Promedio general"
          valor={texto(scorecard.puntajePromedio)}
          contexto={scorecard.muestraSuficiente ? 'Sobre las llamadas elegibles' : 'Muestra insuficiente para promediar'}
        />
        <Indicador
          etiqueta="Tendencia vs. mes anterior"
          valor={textoTendencia(scorecard.tendencia)}
          contexto={scorecard.tendencia === null ? 'No incluida en el plan o sin datos del mes anterior' : 'Puntos de diferencia'}
        />
        <Indicador
          etiqueta="Promedio de la campaña"
          valor={texto(scorecard.promedioCampana)}
          contexto={scorecard.promedioCampana === null ? 'No incluido en el plan o sin muestra suficiente' : 'Para comparar'}
        />
        <Indicador etiqueta="Críticos fallidos" valor={scorecard.criticosFallidos} />
      </div>

      <dl className="flex flex-wrap gap-x-8 gap-y-2 text-sm">
        <div className="flex gap-2">
          <dt className="text-texto-secundario">Evaluadas</dt>
          <dd className="font-medium tabular-nums">{scorecard.llamadasEvaluadas}</dd>
        </div>
        <div className="flex gap-2">
          <dt className="text-texto-secundario">Confirmadas</dt>
          <dd className="font-medium tabular-nums">{scorecard.llamadasConfirmadas}</dd>
        </div>
        <div className="flex gap-2">
          <dt className="text-texto-secundario">Elegibles</dt>
          <dd className="font-medium tabular-nums">{scorecard.llamadasElegibles}</dd>
        </div>
      </dl>

      {scorecard.criterios.length === 0 ? (
        <EstadoVacio titulo="Sin promedios por criterio" descripcion="Aparecen cuando hay suficientes llamadas elegibles en el periodo." />
      ) : (
        <div className="-mx-6 border-t border-borde">
          <Tabla
            descripcion="Promedio por criterio"
            columnas={COLUMNAS}
            filas={scorecard.criterios}
            claveFila={(c) => `${c.rubricaId}-${c.criterioId}`}
          />
        </div>
      )}
    </div>
  )
}
