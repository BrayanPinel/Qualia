import { useState } from 'react'
import { extraerMensajeErrorBackend } from '../../../shared/api/extraerMensajeErrorBackend'
import { Boton } from '../../../shared/components/Boton'
import { Dialogo } from '../../../shared/components/Dialogo'
import { AlertaFormulario } from '../../../shared/components/Formulario'
import { IconoCandado } from '../../../shared/components/Iconos'
import { publicarRubrica } from '../api/rubricasApi'
import type { RubricaDto } from '../types/rubrica.types'

interface PropsPublicar {
  rubrica: RubricaDto
  /** Si hay motivo, el botón se bloquea y el motivo se muestra al lado. */
  motivoBloqueo: string | null
  onPublicada: () => void
}

/** Publica el borrador guardado. Muestra RUBRICA_INVALIDA, LIMITE_PLAN_RUBRICAS o PUBLICACION_CONCURRENTE tal cual. */
export function PublicarRubricaBoton({ rubrica, motivoBloqueo, onPublicada }: PropsPublicar) {
  const [abierto, setAbierto] = useState(false)
  const [enviando, setEnviando] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const cerrar = () => {
    setAbierto(false)
    setError(null)
  }

  const publicar = async () => {
    setEnviando(true)
    setError(null)
    try {
      await publicarRubrica(rubrica.id)
      setAbierto(false)
      onPublicada()
    } catch (e) {
      setError(extraerMensajeErrorBackend(e))
    } finally {
      setEnviando(false)
    }
  }

  return (
    <>
      {motivoBloqueo && <p className="text-meta text-texto-secundario">{motivoBloqueo}</p>}
      <Boton icono={<IconoCandado tamano={18} />} disabled={motivoBloqueo !== null} onClick={() => setAbierto(true)}>
        Publicar versión {rubrica.version}
      </Boton>
      <Dialogo
        abierto={abierto}
        titulo={`Publicar la versión ${rubrica.version}`}
        descripcion="La versión activa actual pasará a Retirada y las llamadas nuevas se evaluarán con esta. Una versión publicada ya no se puede editar."
        onCerrar={cerrar}
      >
        <div className="flex flex-col gap-5">
          <AlertaFormulario mensaje={error} />
          <div className="flex justify-end gap-2">
            <Boton variante="secundario" onClick={cerrar}>
              Cancelar
            </Boton>
            <Boton icono={<IconoCandado tamano={18} />} cargando={enviando} onClick={publicar}>
              Publicar versión {rubrica.version}
            </Boton>
          </div>
        </div>
      </Dialogo>
    </>
  )
}
