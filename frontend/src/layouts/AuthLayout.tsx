import type { ReactNode } from 'react'
import { Logo } from '../shared/components/Logo'

interface PropsAuthLayout {
  titulo: string
  descripcion: string
  children: ReactNode
  /** Formularios largos (registro) usan una columna más ancha. */
  ancho?: 'normal' | 'amplio'
}

/** Pantallas públicas: panel de marca carbón a la izquierda (escritorio) y formulario sobre alabastro. */
export function AuthLayout({ titulo, descripcion, children, ancho = 'normal' }: PropsAuthLayout) {
  return (
    <div className="grid min-h-screen bg-alabastro lg:grid-cols-[minmax(0,5fr)_minmax(0,7fr)]">
      <aside className="hidden flex-col justify-between bg-carbon p-12 text-alabastro lg:flex">
        <div className="self-start">
          <Logo tono="claro" tamano="lg" />
        </div>
        <div>
          <p className="font-titulo text-h1 font-bold">Cada evaluación, con evidencia.</p>
          <span aria-hidden="true" className="mt-6 block h-1.5 w-20 rounded-full bg-piedra" />
          <p className="mt-6 max-w-sm text-sidebar-texto">
            Rúbricas configurables, evaluación con IA y revisión humana de las excepciones. Cada hallazgo cita la
            transcripción.
          </p>
        </div>
        <p className="text-meta text-sidebar-texto">Claridad con criterio.</p>
      </aside>

      <main className="flex items-start justify-center px-4 py-10 sm:px-8 lg:items-center lg:py-12">
        <div className={`w-full ${ancho === 'amplio' ? 'max-w-2xl' : 'max-w-md'}`}>
          <div className="mb-8 lg:hidden">
            <Logo tamano="md" />
          </div>
          <h1 className="font-titulo text-h2 font-bold md:text-h1">{titulo}</h1>
          <p className="mt-2 text-texto-secundario">{descripcion}</p>
          <div className="mt-8">{children}</div>
        </div>
      </main>
    </div>
  )
}
