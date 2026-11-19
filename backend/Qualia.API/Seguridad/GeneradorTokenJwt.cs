using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Qualia.Application.Common.Options;
using Qualia.Application.Common.Seguridad;
using Qualia.Domain.Enums;

namespace Qualia.API.Seguridad;

/// Clave simétrica (HMAC-SHA256) con la que se firman y validan los tokens. Viene de Jwt__Key en el .env.
/// En Development, si falta, se genera una aleatoria en memoria: los tokens dejan de valer al reiniciar.
public sealed class ClaveJwt
{
    public const int LongitudMinima = 32;

    public ClaveJwt(SymmetricSecurityKey clave, bool esTemporal)
    {
        Clave = clave;
        EsTemporal = esTemporal;
    }

    public SymmetricSecurityKey Clave { get; }

    public bool EsTemporal { get; }

    public static ClaveJwt Crear(JwtOptions opciones, bool esDesarrollo)
    {
        if (!string.IsNullOrWhiteSpace(opciones.Key))
        {
            if (Encoding.UTF8.GetByteCount(opciones.Key) < LongitudMinima)
            {
                throw new InvalidOperationException($"Jwt__Key debe tener al menos {LongitudMinima} caracteres.");
            }

            return new ClaveJwt(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opciones.Key)), esTemporal: false);
        }

        if (!esDesarrollo)
        {
            throw new InvalidOperationException("Falta Jwt__Key (backend/Qualia.API/.env o variable de entorno).");
        }

        return new ClaveJwt(new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(64)), esTemporal: true);
    }
}

/// IGeneradorToken con JWT firmado (JWS). Los claims usan los mismos tipos que lee
/// UsuarioActualHttp: NameIdentifier, empresa_id y Role.
public sealed class GeneradorTokenJwt(IOptions<JwtOptions> opciones, ClaveJwt clave, TimeProvider reloj) : IGeneradorToken
{
    private static readonly JsonWebTokenHandler Manejador = new();

    public TokenEmitido EmitirEmpresarial(Guid usuarioId, Guid empresaId, RolEmpresa rol, string nombre) =>
        Emitir(
        [
            new(ClaimsQualia.UsuarioId, usuarioId.ToString()),
            new(ClaimsQualia.EmpresaId, empresaId.ToString()),
            new(ClaimsQualia.Rol, rol.ToString()),
            new(ClaimTypes.Name, nombre),
        ]);

    public TokenEmitido EmitirPlataforma(Guid administradorId, string nombre) =>
        Emitir(
        [
            new(ClaimsQualia.UsuarioId, administradorId.ToString()),
            new(ClaimsQualia.Rol, RolesPlataforma.PlatformAdmin),
            new(ClaimTypes.Name, nombre),
        ]);

    private TokenEmitido Emitir(Claim[] claims)
    {
        var configuracion = opciones.Value;
        var ahora = reloj.GetUtcNow().UtcDateTime;
        var expira = ahora.AddMinutes(configuracion.ExpirationMinutes);

        var token = Manejador.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = configuracion.Issuer,
            Audience = configuracion.Audience,
            Subject = new ClaimsIdentity([.. claims, new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())]),
            IssuedAt = ahora,
            NotBefore = ahora,
            Expires = expira,
            SigningCredentials = new SigningCredentials(clave.Clave, SecurityAlgorithms.HmacSha256),
        });

        return new TokenEmitido(token, expira);
    }
}
