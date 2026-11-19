using System.Text.RegularExpressions;
using Qualia.Application.Features.Procesamiento;

namespace Qualia.Infrastructure.Procesamiento;

/// Enmascarado de datos sensibles por expresiones regulares (RN-14).
/// El orden importa: primero los patrones más específicos.
/// Cubre el texto escrito y la forma en que un transcriptor real entrega lo dictado.
public sealed partial class EnmascaradorDatos : IEnmascaradorDatos
{
    public const string Correo = "[CORREO]";
    public const string Identidad = "[IDENTIDAD]";
    public const string Tarjeta = "[TARJETA]";
    public const string Telefono = "[TELEFONO]";
    public const string Numero = "[NUMERO]";

    public string Enmascarar(string texto)
    {
        if (string.IsNullOrEmpty(texto))
        {
            return texto;
        }

        var resultado = CorreoRegex().Replace(texto, Correo);
        resultado = CorreoDictadoRegex().Replace(resultado, Correo);
        resultado = IdentidadRegex().Replace(resultado, Identidad);
        // Antes que tarjeta/teléfono: dígitos dictados uno a uno no permiten saber de qué dato se trata.
        resultado = DigitosDictadosRegex().Replace(resultado, Numero);
        resultado = TarjetaRegex().Replace(resultado, Tarjeta);
        resultado = TelefonoRegex().Replace(resultado, Telefono);
        return resultado;
    }

    [GeneratedRegex(@"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}")]
    private static partial Regex CorreoRegex();

    // Correo dictado: "gabriela punto mejía arroba example punto com".
    [GeneratedRegex(@"[\p{L}\d_\-]+(?:\s+punto\s+[\p{L}\d_\-]+)*\s+arroba\s+[\p{L}\d_\-]+(?:\s+punto\s+[\p{L}\d_\-]+)+", RegexOptions.IgnoreCase)]
    private static partial Regex CorreoDictadoRegex();

    // DNI hondureño: 0801-1990-12345 (también sin guiones: 13 dígitos).
    [GeneratedRegex(@"\b\d{4}[- ]?\d{4}[- ]?\d{5}\b")]
    private static partial Regex IdentidadRegex();

    // Tarjetas: 13 a 19 dígitos, opcionalmente agrupados con espacios o guiones.
    [GeneratedRegex(@"\b(?:\d[ \-]?){12,18}\d\b")]
    private static partial Regex TarjetaRegex();

    // Teléfonos: 8 dígitos (9876-5432, 9876 5432, 98765432) con prefijo +504 opcional.
    [GeneratedRegex(@"(?:\+?504[ \-]?)?\b\d{4}[ \-]?\d{4}\b")]
    private static partial Regex TelefonoRegex();

    // Dígitos dictados uno a uno ("0 8 0 1 1 9"): 4 o más dígitos sueltos seguidos.
    // No afecta cantidades normales ("300 megas", "400 lempiras").
    [GeneratedRegex(@"\b\d(?:[ \-]\d){3,}\b")]
    private static partial Regex DigitosDictadosRegex();
}
