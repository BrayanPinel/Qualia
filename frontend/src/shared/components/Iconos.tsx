import type { ReactNode, SVGProps } from 'react'

// Única familia de iconos de la app: lineales, trazo 1.75, 24×24 de base y 20 px por defecto.
// Decorativos por defecto (aria-hidden): el texto que los acompaña es el que se lee.

type PropsIcono = SVGProps<SVGSVGElement> & { tamano?: number }

function crearIcono(nombre: string, trazos: ReactNode) {
  function Icono({ tamano = 20, ...props }: PropsIcono) {
    return (
      <svg
        width={tamano}
        height={tamano}
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth={1.75}
        strokeLinecap="round"
        strokeLinejoin="round"
        aria-hidden="true"
        focusable="false"
        {...props}
      >
        {trazos}
      </svg>
    )
  }
  Icono.displayName = nombre
  return Icono
}

export const IconoInicio = crearIcono('IconoInicio', <><path d="M3 10.5 12 3l9 7.5" /><path d="M5 9.5V21h14V9.5" /><path d="M10 21v-6h4v6" /></>)
export const IconoLlamada = crearIcono('IconoLlamada', <><path d="M5 4h4l2 5-2.5 1.5a11 11 0 0 0 5 5L15 13l5 2v4a2 2 0 0 1-2 2A16 16 0 0 1 3 6a2 2 0 0 1 2-2" /></>)
export const IconoRevision = crearIcono('IconoRevision', <><circle cx="12" cy="12" r="9" /><path d="m8.5 12 2.5 2.5 4.5-5" /></>)
export const IconoRubrica = crearIcono('IconoRubrica', <><path d="M9 6h11M9 12h11M9 18h11" /><path d="M4 6h.01M4 12h.01M4 18h.01" /></>)
export const IconoDisputa = crearIcono('IconoDisputa', <><path d="M21 12a8 8 0 0 1-11.5 7.2L4 20l1-4.5A8 8 0 1 1 21 12Z" /><path d="M12 8v4M12 15.5h.01" /></>)
export const IconoGrafica = crearIcono('IconoGrafica', <><path d="M4 20h16" /><path d="M7 16v-5M12 16V6M17 16v-8" /></>)
export const IconoUsuarios = crearIcono('IconoUsuarios', <><circle cx="9" cy="8" r="3.5" /><path d="M2.5 20a6.5 6.5 0 0 1 13 0" /><path d="M16 4.5a3.5 3.5 0 0 1 0 7M18.5 14.5a6.5 6.5 0 0 1 3 5.5" /></>)
export const IconoTarjeta = crearIcono('IconoTarjeta', <><rect x="3" y="5" width="18" height="14" rx="2" /><path d="M3 10h18M7 15h3" /></>)
export const IconoEdificio = crearIcono('IconoEdificio', <><path d="M4 21V5a2 2 0 0 1 2-2h8a2 2 0 0 1 2 2v16" /><path d="M16 9h2a2 2 0 0 1 2 2v10M2 21h20" /><path d="M8 7h4M8 11h4M8 15h4" /></>)
export const IconoSalir = crearIcono('IconoSalir', <><path d="M15 4h3a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2h-3" /><path d="m10 16-4-4 4-4M6 12h10" /></>)
export const IconoMenu = crearIcono('IconoMenu', <><path d="M4 6h16M4 12h16M4 18h16" /></>)
export const IconoCerrar = crearIcono('IconoCerrar', <><path d="M6 6l12 12M18 6 6 18" /></>)
export const IconoCheck = crearIcono('IconoCheck', <><path d="m5 12.5 4.5 4.5L19 7.5" /></>)
export const IconoX = crearIcono('IconoX', <><path d="M6 6l12 12M18 6 6 18" /></>)
export const IconoReloj = crearIcono('IconoReloj', <><circle cx="12" cy="12" r="9" /><path d="M12 7v5l3 2" /></>)
export const IconoAlerta = crearIcono('IconoAlerta', <><path d="M10.3 4.3 2.6 18a2 2 0 0 0 1.7 3h15.4a2 2 0 0 0 1.7-3L13.7 4.3a2 2 0 0 0-3.4 0Z" /><path d="M12 9.5v4M12 17h.01" /></>)
export const IconoInfo = crearIcono('IconoInfo', <><circle cx="12" cy="12" r="9" /><path d="M12 11v5M12 8h.01" /></>)
export const IconoCandado = crearIcono('IconoCandado', <><rect x="4.5" y="10.5" width="15" height="10.5" rx="2" /><path d="M8 10.5V7a4 4 0 0 1 8 0v3.5" /></>)
export const IconoMas = crearIcono('IconoMas', <><path d="M12 5v14M5 12h14" /></>)
export const IconoEditar = crearIcono('IconoEditar', <><path d="M4 20h4L19 9a2.8 2.8 0 0 0-4-4L4 16v4Z" /><path d="m13.5 6.5 4 4" /></>)
export const IconoFlechaIzq = crearIcono('IconoFlechaIzq', <><path d="m15 18-6-6 6-6" /></>)
export const IconoFlechaDer = crearIcono('IconoFlechaDer', <><path d="m9 18 6-6-6-6" /></>)
export const IconoRecargar = crearIcono('IconoRecargar', <><path d="M20 11a8 8 0 1 0-2.3 5.7" /><path d="M20 4v7h-7" /></>)
export const IconoCarpeta = crearIcono('IconoCarpeta', <><path d="M3 7a2 2 0 0 1 2-2h4l2 2h8a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V7Z" /></>)
export const IconoGiro = crearIcono('IconoGiro', <><path d="M12 3a9 9 0 1 0 9 9" /></>)
export const IconoDocumento = crearIcono('IconoDocumento', <><path d="M14 3H7a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V8l-5-5Z" /><path d="M14 3v5h5M9 13h6M9 17h4" /></>)
