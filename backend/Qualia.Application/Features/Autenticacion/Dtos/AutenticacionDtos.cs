using Qualia.Application.Features.Campanas.Dtos;

namespace Qualia.Application.Features.Autenticacion.Dtos;

// Entrada con propiedades anulables: un campo ausente se informa como error de validación.

/// LoginDto { codigoEmpresa, email, password }.
public sealed class LoginDto
{
    public string? CodigoEmpresa { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
}

/// LoginPlataformaDto { email, password }.
public sealed class LoginPlataformaDto
{
    public string? Email { get; set; }
    public string? Password { get; set; }
}

/// SesionDto { accessToken, tokenType, expiraEn, usuario, empresaId }.
public sealed record SesionDto(string AccessToken, string TokenType, DateTime ExpiraEn, UsuarioDto Usuario, Guid EmpresaId);

/// SesionPlataformaDto { accessToken, tokenType, expiraEn, administradorId, rol }.
/// AdministradorId es el Administrador global, no un Usuario de empresa.
public sealed record SesionPlataformaDto(string AccessToken, string TokenType, DateTime ExpiraEn, Guid AdministradorId, string Rol);
