using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Qualia.Application.Common.Errores;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Common.Seguridad;
using Qualia.Application.Features.Autenticacion.Dtos;
using Qualia.Application.Features.Campanas.Dtos;
using Qualia.Domain.Enums;
using Qualia.Domain.Servicios;

namespace Qualia.Application.Features.Autenticacion;

/// <summary>
/// POST /api/auth/login (usuarios de empresa) y POST /api/plataforma/auth/login (Administrador global).
/// Empresa inexistente, correo inexistente y contraseña incorrecta responden el mismo 401, y en los tres
/// casos se calcula un hash para que el tiempo de respuesta no delate cuál falló.
/// Cuenta inactiva o empresa suspendida (403) solo se informa tras una contraseña correcta.
/// </summary>
public sealed class InicioSesion(
    IQualiaDbContext db,
    IValidator<LoginDto> validadorLogin,
    IValidator<LoginPlataformaDto> validadorPlataforma,
    IServicioHashContrasena hash,
    IGeneradorToken tokens)
{
    public const string CodigoCredencialesInvalidas = "CREDENCIALES_INVALIDAS";
    public const string CodigoCuentaInactiva = "CUENTA_INACTIVA";
    public const string CodigoEmpresaSuspendida = "EMPRESA_SUSPENDIDA";
    private const string TipoToken = "Bearer";

    private static string? _hashFicticio;

    public async Task<SesionDto> IniciarEmpresarialAsync(LoginDto dto, CancellationToken ct)
    {
        await validadorLogin.ValidarOLanzarAsync(dto, ct);

        var codigo = NormalizadorIdentidad.Codigo(dto.CodigoEmpresa!);
        var correo = NormalizadorIdentidad.Correo(dto.Email!);

        var empresa = await db.Empresas.AsNoTracking().FirstOrDefaultAsync(e => e.Codigo == codigo, ct);

        // Búsqueda de identidad previa a la sesión: aún no hay empresa en contexto, se filtra por la encontrada.
        var usuario = empresa is null
            ? null
            : await db.Usuarios.IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.EmpresaId == empresa.Id && u.Correo == correo, ct);

        var verificacion = hash.Verificar(usuario?.PasswordHash ?? HashFicticio(), dto.Password!);
        if (empresa is null || usuario is null || verificacion == ResultadoVerificacionContrasena.Incorrecta)
        {
            throw CredencialesInvalidas();
        }

        if (!usuario.Activo)
        {
            throw ExcepcionNegocio.Prohibido(CodigoCuentaInactiva, "La cuenta está desactivada.");
        }

        if (empresa.Estado == EstadoEmpresa.Suspendida)
        {
            throw ExcepcionNegocio.Prohibido(CodigoEmpresaSuspendida, "La empresa está suspendida.");
        }

        if (verificacion == ResultadoVerificacionContrasena.CorrectaRequiereRehash)
        {
            usuario.PasswordHash = hash.Hashear(dto.Password!);
            await db.SaveChangesAsync(ct);
        }

        var token = tokens.EmitirEmpresarial(usuario.Id, usuario.EmpresaId, usuario.Rol, usuario.Nombre);
        var datos = new UsuarioDto(usuario.Id, usuario.EmpresaId, usuario.Nombre, usuario.Correo, usuario.Rol, usuario.Activo);
        return new SesionDto(token.AccessToken, TipoToken, token.ExpiraEn, datos, usuario.EmpresaId);
    }

    public async Task<SesionPlataformaDto> IniciarPlataformaAsync(LoginPlataformaDto dto, CancellationToken ct)
    {
        await validadorPlataforma.ValidarOLanzarAsync(dto, ct);

        var correo = NormalizadorIdentidad.Correo(dto.Email!);
        var administrador = await db.Administradores.FirstOrDefaultAsync(a => a.Correo == correo, ct);

        var verificacion = hash.Verificar(administrador?.PasswordHash ?? HashFicticio(), dto.Password!);
        if (administrador is null || verificacion == ResultadoVerificacionContrasena.Incorrecta)
        {
            throw CredencialesInvalidas();
        }

        if (!administrador.Activo)
        {
            throw ExcepcionNegocio.Prohibido(CodigoCuentaInactiva, "La cuenta está desactivada.");
        }

        if (verificacion == ResultadoVerificacionContrasena.CorrectaRequiereRehash)
        {
            administrador.PasswordHash = hash.Hashear(dto.Password!);
            await db.SaveChangesAsync(ct);
        }

        var token = tokens.EmitirPlataforma(administrador.Id, administrador.Nombre);
        return new SesionPlataformaDto(token.AccessToken, TipoToken, token.ExpiraEn, administrador.Id, RolesPlataforma.PlatformAdmin);
    }

    /// <summary>Hash válido de una contraseña aleatoria: iguala el costo cuando la cuenta no existe.</summary>
    private string HashFicticio() => _hashFicticio ??= hash.Hashear(Guid.NewGuid().ToString("N"));

    private static ExcepcionNegocio CredencialesInvalidas() =>
        new(401, CodigoCredencialesInvalidas, "Credenciales inválidas.");
}
