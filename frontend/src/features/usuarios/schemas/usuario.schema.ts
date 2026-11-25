import { z } from 'zod'
import { contrasenaNueva, emailObligatorio, nombreEntre3y100 } from '../../../shared/schemas/camposComunes'
import { ROLES_EMPRESA, type RolEmpresa } from '../../../shared/types/comunes.types'

// Espejo de CrearUsuarioValidator y ActualizarUsuarioValidator.

const nombre = nombreEntre3y100('El nombre es obligatorio.')

export const crearUsuarioSchema = z.object({
  nombre,
  email: emailObligatorio,
  password: contrasenaNueva,
  rol: z
    .string()
    .min(1, 'El rol es obligatorio.')
    .refine((valor) => ROLES_EMPRESA.includes(valor as RolEmpresa), 'El rol no es válido.')
    .transform((valor) => valor as RolEmpresa),
})

export const actualizarUsuarioSchema = z.object({
  nombre,
  activo: z.boolean({ error: 'Indique si el usuario está activo.' }),
})

export type CrearUsuarioEntrada = z.input<typeof crearUsuarioSchema>
export type CrearUsuarioFormulario = z.output<typeof crearUsuarioSchema>
export type ActualizarUsuarioFormulario = z.infer<typeof actualizarUsuarioSchema>
