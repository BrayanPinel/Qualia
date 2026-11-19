namespace Qualia.Application.Common.Options;

/// Configuración del token JWT. La clave real se obtiene de variables de
/// entorno o user-secrets y nunca debe guardarse en appsettings.
/// La configuración del esquema de autenticación se completa en A06.
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public string Key { get; init; } = string.Empty;

    public int ExpirationMinutes { get; init; } = 60;
}
