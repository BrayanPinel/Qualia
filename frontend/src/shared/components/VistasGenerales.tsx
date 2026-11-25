import { Link } from 'react-router-dom'
import { EncabezadoPagina } from './EncabezadoPagina'
import { EstadoVacio } from './EstadosVista'
import { Tarjeta } from './Tarjeta'

/**
 * Marcador de una ruta cuyo módulo aún no está integrado. Cuando el integrante responsable
 * entrega su vista, en App.tsx se reemplaza este componente por la vista real.
 */
export function ModuloPendienteView({ titulo }: { titulo: string }) {
  return (
    <>
      <EncabezadoPagina titulo={titulo} />
      <Tarjeta>
        <EstadoVacio
          titulo="Módulo en desarrollo"
          descripcion="Esta sección todavía no está disponible en el frontend. Los datos ya existen en el backend."
        />
      </Tarjeta>
    </>
  )
}

export function NoEncontradaView() {
  return (
    <main className="grid min-h-screen place-items-center bg-alabastro px-4">
      <div className="max-w-md text-center">
        <p className="text-meta font-medium tracking-widest text-texto-secundario uppercase">Error 404</p>
        <h1 className="mt-2 font-titulo text-h1 font-bold">Página no encontrada</h1>
        <p className="mt-3 text-texto-secundario">La dirección no existe o fue movida.</p>
        <Link
          to="/"
          className="mt-8 inline-flex h-11 items-center rounded-lg bg-oliva px-4 text-sm font-medium text-superficie hover:bg-oliva-hover"
        >
          Ir al inicio
        </Link>
      </div>
    </main>
  )
}
