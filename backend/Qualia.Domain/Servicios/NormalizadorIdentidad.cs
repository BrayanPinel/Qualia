using System.Text;

namespace Qualia.Domain.Servicios;

/// Forma canónica del código de empresa y de los correos. Se aplica al guardar y al consultar
/// (registro, alta de usuarios e inicio de sesión) para que las restricciones únicas
/// empresa.codigo, administrador.correo y usuario(empresa_id, correo)
/// no dependan de mayúsculas ni espacios.
public static class NormalizadorIdentidad
{
    public static string Codigo(string codigo) => Normalizar(codigo, nameof(codigo));

    public static string Correo(string correo) => Normalizar(correo, nameof(correo));

    private static string Normalizar(string valor, string parametro)
    {
        ArgumentNullException.ThrowIfNull(valor, parametro);

        // NFC unifica acentos compuestos y precompuestos antes de pasar a minúsculas.
        return valor.Trim().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }
}
