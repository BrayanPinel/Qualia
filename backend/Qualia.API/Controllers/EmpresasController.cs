using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qualia.Application.Common.Errores;
using Qualia.Application.Features.Empresas;
using Qualia.Application.Features.Empresas.Dtos;

namespace Qualia.API.Controllers;

/// <summary>Registro público de empresas (HU-01).</summary>
[ApiController]
[Route("api/empresas")]
[Produces("application/json")]
public sealed class EmpresasController(RegistroEmpresa registro) : ControllerBase
{
    /// <summary>
    /// Crea la empresa, su usuario AdminEmpresa y la suscripción de prueba del plan elegido
    /// en una sola transacción.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType<EmpresaRegistradaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EmpresaRegistradaDto>> Registrar([FromBody] RegistrarEmpresaDto dto, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await registro.RegistrarAsync(dto, ct));
}
