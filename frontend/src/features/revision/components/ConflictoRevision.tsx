import { Boton } from '../../../shared/components/Boton'
import { IconoAlerta, IconoRecargar } from '../../../shared/components/Iconos'

interface PropsConflicto {
  mensaje: string
  onRecargar: () => void
}

/** Otro supervisor (u otra pestaña) decidió primero: mensaje del backend y recarga de la evaluación vigente. */
export function ConflictoRevision({ mensaje, onRecargar }: PropsConflicto) {
  return (
    <div role="alert" className="flex flex-col gap-3 rounded-lg border border-alerta/30 bg-alerta-fondo px-4 py-3 text-alerta sm:flex-row sm:items-center sm:justify-between">
      <p className="flex items-start gap-2 text-sm font-medium">
        <IconoAlerta tamano={18} className="mt-0.5 shrink-0" />
        {mensaje}
      </p>
      <Boton variante="secundario" tamano="sm" icono={<IconoRecargar tamano={16} />} onClick={onRecargar} className="self-start sm:self-auto">
        Recargar evaluación
      </Boton>
    </div>
  )
}
