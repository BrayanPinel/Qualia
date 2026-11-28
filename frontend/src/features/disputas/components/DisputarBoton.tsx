import { useState } from 'react'
import { Link } from 'react-router-dom'
import { Boton } from '../../../shared/components/Boton'
import { Dialogo } from '../../../shared/components/Dialogo'
import { IconoDisputa } from '../../../shared/components/Iconos'
import type { DisputaDto } from '../types/disputa.types'
import { DisputarForm } from './DisputarForm'

/**
 * El Agente disputa su evaluación oficial. Se muestra en la tarjeta de evaluación del detalle de la llamada.
 * Mientras la disputa está abierta, la evaluación no cuenta en los promedios (RN-11).
 */
export function DisputarBoton({ evaluacionId }: { evaluacionId: string }) {
  const [abierto, setAbierto] = useState(false)
  const [creada, setCreada] = useState<DisputaDto | null>(null)

  const cerrar = () => setAbierto(false)

  return (
    <>
      <Boton variante="secundario" tamano="sm" icono={<IconoDisputa tamano={18} />} onClick={() => setAbierto(true)}>
        Disputar
      </Boton>
      <Dialogo
        abierto={abierto}
        titulo={creada ? 'Disputa enviada' : 'Disputar evaluación'}
        descripcion={creada ? undefined : 'Tu supervisor revisará el audio y mantendrá o modificará las notas.'}
        onCerrar={cerrar}
      >
        {abierto && !creada && <DisputarForm evaluacionId={evaluacionId} onCreada={setCreada} onCancelar={cerrar} />}
        {creada && (
          <div className="flex flex-col gap-5">
            <p className="text-sm">Mientras esté abierta, esta evaluación no cuenta en tus promedios.</p>
            <div className="flex justify-end gap-2">
              <Boton variante="secundario" onClick={cerrar}>
                Cerrar
              </Boton>
              <Link
                to={`/disputas/${creada.id}`}
                className="inline-flex h-11 items-center rounded-lg bg-oliva px-4 text-sm font-medium text-superficie hover:bg-oliva-hover"
              >
                Ver la disputa
              </Link>
            </div>
          </div>
        )}
      </Dialogo>
    </>
  )
}
