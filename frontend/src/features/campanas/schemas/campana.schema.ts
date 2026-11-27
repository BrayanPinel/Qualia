import { z } from 'zod'
import { nombreEntre3y100 } from '../../../shared/schemas/camposComunes'

// Espejo de GuardarCampanaValidator.

export const guardarCampanaSchema = z.object({
  nombre: nombreEntre3y100('El nombre es obligatorio.'),
  activa: z.boolean({ error: 'Indique si la campaña está activa.' }),
})

export type GuardarCampanaFormulario = z.infer<typeof guardarCampanaSchema>

/** Selector de la persona a asignar: el backend recibe el id en la ruta. */
export const asignarUsuarioSchema = z.object({
  usuarioId: z.string().min(1, 'Seleccione a la persona que quiere asignar.'),
})

export type AsignarUsuarioFormulario = z.infer<typeof asignarUsuarioSchema>
