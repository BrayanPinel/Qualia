import { z } from 'zod'
import { contrasenaNueva, emailObligatorio, nombreEntre3y100 } from '../../../shared/schemas/camposComunes'

// Espejo de RegistrarEmpresaValidator: código de 3 a 40 (letras, números y guiones),
// nombres de 3 a 100, email válido ≤ 254 y contraseña de 12 a 128.

export const registroEmpresaSchema = z.object({
  codigoEmpresa: z
    .string()
    .trim()
    .min(1, 'El código de empresa es obligatorio.')
    .min(3, 'El código debe tener entre 3 y 40 caracteres.')
    .max(40, 'El código debe tener entre 3 y 40 caracteres.')
    .regex(/^[A-Za-z0-9-]+$/, 'El código solo admite letras, números y guiones.'),
  nombreEmpresa: nombreEntre3y100('El nombre de la empresa es obligatorio.'),
  planId: z.string().min(1, 'El plan es obligatorio.'),
  nombreAdministrador: nombreEntre3y100('El nombre del administrador es obligatorio.'),
  email: emailObligatorio,
  password: contrasenaNueva,
})

export type RegistroEmpresaFormulario = z.infer<typeof registroEmpresaSchema>
