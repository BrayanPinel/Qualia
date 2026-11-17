using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Common.Resultados;
using Qualia.Infrastructure.Multitenancy;

namespace Qualia.API.Common;

/// <summary>
/// Extensiones de presentación HTTP para traducir <see cref="Resultado{T}"/> al contrato
/// uniforme <see cref="ErrorDto"/> (fase1 §7.7 y §7.10) y sincronizar el contexto de empresa
/// desde los claims del JWT autenticado cuando aún no haya sido establecido por el middleware de Rol A.
/// </summary>
public static class ResultadoExtensions
{
    private static readonly string[] ClaimsEmpresa = ["empresaId", "empresa_id", "tenantId"];
    private static readonly string[] ClaimsAdministrador =
    [
        "administradorId",
        "administrador_id",
        ClaimTypes.NameIdentifier,
        "sub",
    ];

    public static IActionResult AActionResult<T>(this ControllerBase controller, Resultado<T> resultado)
    {
        if (resultado.EsExitoso)
        {
            return controller.Ok(resultado.Valor);
        }

        return controller.AErrorActionResult(resultado.Error);
    }

    public static IActionResult AErrorActionResult(this ControllerBase controller, ErrorAplicacion error)
    {
        var traceId = controller.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString("N");
        var dto = error.AErrorDto(traceId);

        return new ObjectResult(dto)
        {
            StatusCode = dto.Status,
        };
    }

    public static void SincronizarContextoEmpresaDesdeClaims(
        this ControllerBase controller,
        IContextoEmpresa contextoEmpresa)
    {
        if (contextoEmpresa.EmpresaId is { } actual && actual != Guid.Empty)
        {
            return;
        }

        if (contextoEmpresa is not ContextoEmpresaAmbiente ambiente)
        {
            return;
        }

        var user = controller.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        foreach (var tipoClaim in ClaimsEmpresa)
        {
            var valor = user.FindFirst(tipoClaim)?.Value;
            if (Guid.TryParse(valor, out var empresaId) && empresaId != Guid.Empty)
            {
                ambiente.EstablecerEmpresa(empresaId);
                return;
            }
        }
    }

    public static Guid ObtenerAdministradorIdDesdeClaims(this ControllerBase controller)
    {
        var user = controller.HttpContext?.User;
        if (user is null)
        {
            return Guid.Empty;
        }

        foreach (var tipoClaim in ClaimsAdministrador)
        {
            var valor = user.FindFirst(tipoClaim)?.Value;
            if (Guid.TryParse(valor, out var adminId) && adminId != Guid.Empty)
            {
                return adminId;
            }
        }

        return Guid.Empty;
    }
}
