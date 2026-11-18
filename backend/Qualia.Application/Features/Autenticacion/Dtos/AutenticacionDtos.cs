using Qualia.Application.Features.Campanas.Dtos;

namespace Qualia.Application.Features.Autenticacion.Dtos;

// Entrada con propiedades anulables: un campo ausente se informa como error de validación.

/// <summary>LoginDto { codigoEmpresa, email, password }.</summary>
public sealed class LoginDto
{
    public string? CodigoEmpresa { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
}

/// <summary>LoginPlataformaDto { email, password }.</summary>
public sealed class LoginPlataformaDto
{
    public string? Email { get; set; }
    public string? Password { get; set; }
}

/// <summary>SesionDto { accessToken, tokenType, expiraEn, usuario, empresaId }.</summary>
public sealed record SesionDto(string AccessToken, string TokenType, DateTime ExpiraEn, UsuarioDto Usuario, Guid EmpresaId);

/// <summary>
/// SesionPlataformaDto { accessToken, tokenType, expiraEn, administradorId, rol }.
/// <see cref="AdministradorId"/> es el Administrador global, no un Usuario de empresa.
/// </summary>
public sealed record SesionPlataformaDto(string AccessToken, string TokenType, DateTime ExpiraEn, Guid AdministradorId, string Rol);
