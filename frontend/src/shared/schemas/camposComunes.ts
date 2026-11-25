import { z } from 'zod'

// Reglas repetidas en varios validadores del backend (RegistrarEmpresaValidator, CrearUsuarioValidator…),
// con los mismos mensajes.

const formatoEmail = z.email()

export const emailObligatorio = z
  .string()
  .trim()
  .min(1, 'El email es obligatorio.')
  .max(254, 'El email admite como máximo 254 caracteres.')
  .refine((valor) => formatoEmail.safeParse(valor).success, 'El email no es válido.')

/** Contraseña de 12 a 128 caracteres (MinimoContrasena / MaximoContrasena). */
export const contrasenaNueva = z
  .string()
  .min(1, 'La contraseña es obligatoria.')
  .min(12, 'La contraseña debe tener entre 12 y 128 caracteres.')
  .max(128, 'La contraseña debe tener entre 12 y 128 caracteres.')

/** Nombre de 3 a 100 caracteres sin contar espacios de los extremos. */
export function nombreEntre3y100(mensajeObligatorio: string) {
  return z
    .string()
    .trim()
    .min(1, mensajeObligatorio)
    .min(3, 'El nombre debe tener entre 3 y 100 caracteres.')
    .max(100, 'El nombre debe tener entre 3 y 100 caracteres.')
}
