using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Qualia.Application.Common.Errores;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Common.Options;
using Qualia.Application.Common.Seguridad;
using Qualia.Domain.Enums;

namespace Qualia.API.Seguridad;

/// Autenticación de la API: JWT Bearer siempre. Solo en Development con Autenticacion:ModoDemo=true
/// se acepta además la cabecera X-Usuario-Demo (la usan las pruebas de integración y los .http del equipo).
/// Un esquema de política elige el manejador según la petición.
public static class ConfiguracionAutenticacion
{
    public const string Esquema = "Qualia";
    private const string ClaveErrorToken = "qualia.error_token";

    public static IServiceCollection AgregarAutenticacionQualia(this IServiceCollection services, IConfiguration configuracion, IHostEnvironment entorno)
    {
        var opciones = configuracion.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        var clave = ClaveJwt.Crear(opciones, entorno.IsDevelopment());
        var modoDemo = entorno.IsDevelopment() && configuracion.GetValue<bool>("Autenticacion:ModoDemo");

        services.Configure<JwtOptions>(configuracion.GetSection(JwtOptions.SectionName));
        services.AddSingleton(clave);
        services.AddSingleton<IGeneradorToken, GeneradorTokenJwt>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, RespuestaAutorizacion>();
        services.AddHostedService(sp => new AvisoClaveJwt(sp.GetRequiredService<ILogger<AvisoClaveJwt>>(), clave.EsTemporal));

        var autenticacion = services.AddAuthentication(Esquema)
            .AddPolicyScheme(Esquema, Esquema, o => o.ForwardDefaultSelector = contexto =>
                modoDemo && contexto.Request.Headers.ContainsKey(AutenticacionDemoHandler.Cabecera)
                    ? AutenticacionDemoHandler.Esquema
                    : JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                // Los claims llegan con el mismo nombre con el que se emitieron (sin traducción automática).
                o.MapInboundClaims = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = opciones.Issuer,
                    ValidateAudience = true,
                    ValidAudience = opciones.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = clave.Clave,
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimsQualia.Rol,
                };
                o.Events = new JwtBearerEvents
                {
                    // El token es válido, pero la cuenta o la empresa pudo desactivarse después de emitirlo.
                    OnTokenValidated = async contexto =>
                    {
                        if (!await SesionVigenteAsync(contexto.Principal!, contexto.HttpContext))
                        {
                            contexto.HttpContext.Items[ClaveErrorToken] = "SESION_REVOCADA";
                            contexto.Fail("La cuenta o la empresa ya no está activa.");
                        }
                    },
                    OnAuthenticationFailed = contexto =>
                    {
                        contexto.HttpContext.Items[ClaveErrorToken] =
                            contexto.HttpContext.Items[ClaveErrorToken] as string
                            ?? (contexto.Exception is SecurityTokenExpiredException ? "TOKEN_EXPIRADO" : "TOKEN_INVALIDO");
                        return Task.CompletedTask;
                    },
                };
            });

        if (modoDemo)
        {
            autenticacion.AddScheme<AuthenticationSchemeOptions, AutenticacionDemoHandler>(AutenticacionDemoHandler.Esquema, null);
        }

        services.AddAuthorization();
        return services;
    }

    /// Comprueba en la base de datos que el usuario (y su empresa) o el administrador global siga activo.
    /// Así una desactivación surte efecto en la siguiente petición y no al expirar el token.
    private static async Task<bool> SesionVigenteAsync(ClaimsPrincipal principal, HttpContext http)
    {
        if (!Guid.TryParse(principal.FindFirst(ClaimsQualia.UsuarioId)?.Value, out var id))
        {
            return false;
        }

        var db = http.RequestServices.GetRequiredService<IQualiaDbContext>();
        var ct = http.RequestAborted;

        if (principal.FindFirst(ClaimsQualia.Rol)?.Value == RolesPlataforma.PlatformAdmin)
        {
            return await db.Administradores.AsNoTracking().AnyAsync(a => a.Id == id && a.Activo, ct);
        }

        if (!Guid.TryParse(principal.FindFirst(ClaimsQualia.EmpresaId)?.Value, out var empresaId))
        {
            return false;
        }

        // Búsqueda de identidad previa a fijar la empresa del contexto: se filtra explícitamente por la del token.
        return await db.Usuarios.IgnoreQueryFilters().AsNoTracking()
                   .AnyAsync(u => u.EmpresaId == empresaId && u.Id == id && u.Activo, ct)
               && await db.Empresas.AsNoTracking()
                   .AnyAsync(e => e.Id == empresaId && e.Estado == EstadoEmpresa.Activa, ct);
    }

    /// 401 y 403 del pipeline ([Authorize]) con el mismo ErrorDto que el resto de la API,
    /// en lugar de una respuesta vacía.
    private sealed class RespuestaAutorizacion : IAuthorizationMiddlewareResultHandler
    {
        private readonly AuthorizationMiddlewareResultHandler _predeterminado = new();

        public async Task HandleAsync(RequestDelegate siguiente, HttpContext contexto, AuthorizationPolicy politica, PolicyAuthorizationResult resultado)
        {
            if (resultado.Challenged)
            {
                var codigo = contexto.Items[ClaveErrorToken] as string ?? "NO_AUTENTICADO";
                var mensaje = codigo switch
                {
                    "TOKEN_EXPIRADO" => "La sesión expiró; inicie sesión de nuevo.",
                    "TOKEN_INVALIDO" => "El token de acceso no es válido.",
                    "SESION_REVOCADA" => "La cuenta o la empresa ya no está activa; inicie sesión de nuevo.",
                    _ => "Se requiere una sesión válida.",
                };
                contexto.Response.Headers.WWWAuthenticate = "Bearer";
                await EscribirAsync(contexto, StatusCodes.Status401Unauthorized, codigo, mensaje);
                return;
            }

            if (resultado.Forbidden)
            {
                await EscribirAsync(contexto, StatusCodes.Status403Forbidden, "ROL_NO_AUTORIZADO", "Su rol no permite realizar esta operación.");
                return;
            }

            await _predeterminado.HandleAsync(siguiente, contexto, politica, resultado);
        }

        private static Task EscribirAsync(HttpContext contexto, int status, string codigo, string mensaje)
        {
            contexto.Response.StatusCode = status;
            var traceId = Activity.Current?.Id ?? contexto.TraceIdentifier;
            return contexto.Response.WriteAsJsonAsync(new ErrorDto(status, codigo, mensaje, null, traceId));
        }
    }
}

/// Advierte en el log cuando la clave JWT es temporal (solo Development).
internal sealed class AvisoClaveJwt(ILogger<AvisoClaveJwt> logger, bool temporal) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (temporal)
        {
            logger.LogWarning("Falta Jwt__Key en el .env: se usa una clave temporal; los tokens dejan de valer al reiniciar la API.");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
