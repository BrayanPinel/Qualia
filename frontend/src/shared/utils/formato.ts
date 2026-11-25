// Formatos de presentación en español (coma decimal, fechas cortas).

const numero = new Intl.NumberFormat('es', { maximumFractionDigits: 2 })
const dinero = new Intl.NumberFormat('es', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
const fecha = new Intl.DateTimeFormat('es', { day: 'numeric', month: 'short', year: 'numeric' })

export const formatearNumero = (valor: number) => numero.format(valor)
export const formatearPrecio = (valor: number) => dinero.format(valor)
export const formatearFecha = (iso: string) => fecha.format(new Date(iso))

/** Fecha ISO a "YYYY-MM-DD" (UTC) para un input type="date". */
export const aFechaInput = (iso: string) => iso.slice(0, 10)
