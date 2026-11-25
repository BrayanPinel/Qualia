import { z } from 'zod'

// Espejo de LoginValidator y LoginPlataformaValidator: solo presencia y tope de longitud,
// con los mismos mensajes que el backend.

const email = z
  .string()
  .trim()
  .min(1, 'El email es obligatorio.')
  .max(254, 'El email admite como máximo 254 caracteres.')

const password = z
  .string()
  .min(1, 'La contraseña es obligatoria.')
  .max(128, 'La contraseña admite como máximo 128 caracteres.')

export const loginSchema = z.object({
  codigoEmpresa: z
    .string()
    .trim()
    .min(1, 'El código de empresa es obligatorio.')
    .max(40, 'El código admite como máximo 40 caracteres.'),
  email,
  password,
})

export const loginPlataformaSchema = z.object({ email, password })

export type LoginFormulario = z.infer<typeof loginSchema>
export type LoginPlataformaFormulario = z.infer<typeof loginPlataformaSchema>
