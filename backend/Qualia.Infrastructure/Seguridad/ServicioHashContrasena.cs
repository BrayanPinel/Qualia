using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Qualia.Application.Common.Seguridad;

namespace Qualia.Infrastructure.Seguridad;

/// <summary>Configuración del hash de contraseñas (sección Seguridad:HashContrasena).</summary>
public sealed class OpcionesHashContrasena
{
    public const string Seccion = "Seguridad:HashContrasena";

    /// <summary>Por debajo de este valor el servicio no se crea: protege de una configuración débil por error.</summary>
    public const int MinimoIteraciones = 100_000;

    /// <summary>Iteraciones de PBKDF2-HMAC-SHA512; 210.000 es la recomendación de OWASP.</summary>
    public int Iteraciones { get; set; } = 210_000;
}

/// <summary>
/// <see cref="IServicioHashContrasena"/> con el <see cref="PasswordHasher{TUser}"/> de Microsoft (PBKDF2).
/// A diferencia de BCrypt, no trunca a 72 bytes, así que admite las contraseñas de hasta 128 caracteres
/// del contrato. El hash guarda versión, iteraciones y sal, lo que permite subir las iteraciones
/// sin invalidar las contraseñas existentes.
/// </summary>
public sealed class ServicioHashContrasena : IServicioHashContrasena
{
    private readonly PasswordHasher<Credencial> _hasher;

    public ServicioHashContrasena(IOptions<OpcionesHashContrasena> opciones)
    {
        var iteraciones = opciones.Value.Iteraciones;
        if (iteraciones < OpcionesHashContrasena.MinimoIteraciones)
        {
            throw new InvalidOperationException(
                $"{OpcionesHashContrasena.Seccion}:Iteraciones debe ser al menos {OpcionesHashContrasena.MinimoIteraciones}.");
        }

        _hasher = new PasswordHasher<Credencial>(Options.Create(new PasswordHasherOptions
        {
            CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3,
            IterationCount = iteraciones,
        }));
    }

    public string Hashear(string contrasena)
    {
        ArgumentException.ThrowIfNullOrEmpty(contrasena);
        return _hasher.HashPassword(Credencial.Instancia, contrasena);
    }

    public ResultadoVerificacionContrasena Verificar(string hash, string contrasena)
    {
        if (string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(contrasena))
        {
            return ResultadoVerificacionContrasena.Incorrecta;
        }

        try
        {
            return _hasher.VerifyHashedPassword(Credencial.Instancia, hash, contrasena) switch
            {
                PasswordVerificationResult.Success => ResultadoVerificacionContrasena.Correcta,
                PasswordVerificationResult.SuccessRehashNeeded => ResultadoVerificacionContrasena.CorrectaRequiereRehash,
                _ => ResultadoVerificacionContrasena.Incorrecta,
            };
        }
        catch (FormatException)
        {
            // Valor que no es un hash (por ejemplo, el marcador de los usuarios demo sin credenciales).
            return ResultadoVerificacionContrasena.Incorrecta;
        }
    }

    /// <summary>PasswordHasher exige un tipo de usuario, pero el algoritmo no lo usa.</summary>
    private sealed class Credencial
    {
        public static readonly Credencial Instancia = new();
    }
}
