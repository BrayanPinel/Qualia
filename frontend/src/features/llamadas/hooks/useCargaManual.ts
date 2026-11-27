import { useState } from 'react'
import type { Rol } from '../../../shared/types/comunes.types'

// La carga manual solo existe en Development. Cuando el backend responde 403 CARGA_MANUAL_DESHABILITADA
// se recuerda en la pestaña (sessionStorage) para no volver a mostrar el botón.
const CLAVE = 'qualia.cargaManualDeshabilitada'

/** AdminEmpresa y Supervisor asignado pueden cargar; el botón se oculta tras el 403 del backend. */
export function useCargaManual(rol: Rol) {
  const [aviso, setAviso] = useState<string | null>(() => sessionStorage.getItem(CLAVE))
  const permitida = rol === 'AdminEmpresa' || rol === 'Supervisor'

  const deshabilitar = (mensaje: string) => {
    sessionStorage.setItem(CLAVE, mensaje)
    setAviso(mensaje)
  }

  return { visible: permitida && aviso === null, aviso: permitida ? aviso : null, deshabilitar }
}
