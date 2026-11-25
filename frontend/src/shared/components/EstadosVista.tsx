import type { ReactNode } from 'react'
import { Boton } from './Boton'
import { IconoAlerta, IconoCarpeta, IconoGiro, IconoRecargar } from './Iconos'

// Estados de carga, error y vacío que toda lista o detalle debe cubrir.

export function EstadoCarga({ texto = 'Cargando…' }: { texto?: string }) {
  return (
    <div role="status" className="flex items-center justify-center gap-3 py-12 text-sm text-texto-secundario">
      <IconoGiro className="animate-spin text-oliva" />
      {texto}
    </div>
  )
}

interface PropsEstadoError {
  /** Mensaje real del backend (extraerMensajeErrorBackend), nunca uno inventado. */
  mensaje: string
  onReintentar?: () => void
}

export function EstadoError({ mensaje, onReintentar }: PropsEstadoError) {
  return (
    <div role="alert" className="flex flex-col items-center gap-4 px-6 py-12 text-center">
      <span className="grid size-12 place-items-center rounded-full bg-error-fondo text-error">
        <IconoAlerta tamano={22} />
      </span>
      <div>
        <p className="font-medium">No se pudo cargar la información</p>
        <p className="mt-1 max-w-md text-sm text-texto-secundario">{mensaje}</p>
      </div>
      {onReintentar && (
        <Boton variante="secundario" icono={<IconoRecargar tamano={18} />} onClick={onReintentar}>
          Reintentar
        </Boton>
      )}
    </div>
  )
}

interface PropsEstadoVacio {
  titulo: string
  descripcion?: string
  accion?: ReactNode
}

export function EstadoVacio({ titulo, descripcion, accion }: PropsEstadoVacio) {
  return (
    <div className="flex flex-col items-center gap-4 px-6 py-12 text-center">
      <span className="grid size-12 place-items-center rounded-full bg-piedra text-texto-secundario">
        <IconoCarpeta tamano={22} />
      </span>
      <div>
        <p className="font-medium">{titulo}</p>
        {descripcion && <p className="mt-1 max-w-md text-sm text-texto-secundario">{descripcion}</p>}
      </div>
      {accion}
    </div>
  )
}
