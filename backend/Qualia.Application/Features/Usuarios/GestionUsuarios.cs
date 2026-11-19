using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Qualia.Application.Common.Errores;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Common.Models;
using Qualia.Application.Common.Seguridad;
using Qualia.Application.Features.Campanas.Dtos;
using Qualia.Application.Features.Usuarios.Dtos;
using Qualia.Domain.Entidades;
using Qualia.Domain.Enums;
using Qualia.Domain.Servicios;

namespace Qualia.Application.Features.Usuarios;

/// GET/POST /api/usuarios y PUT /api/usuarios/{id} (HU-12), solo AdminEmpresa.
/// El filtro global limita todo a la empresa de la sesión: un usuario de otra empresa responde 404.
/// Las escrituras bloquean la empresa (IBloqueoEmpresa) para que el conteo de
/// AdminEmpresa activos y de supervisores no se rompa con peticiones concurrentes.
public sealed class GestionUsuarios(
    IQualiaDbContext db,
    IUsuarioActual usuarioActual,
    IValidator<CrearUsuarioDto> validadorCrear,
    IValidator<ActualizarUsuarioDto> validadorActualizar,
    IValidator<PaginacionDto> validadorPaginacion,
    IServicioHashContrasena hash,
    IConsultaPlanService plan,
    IBloqueoEmpresa bloqueo,
    IRegistroAuditoria auditoria)
{
    public const string CodigoCorreoDuplicado = "CORREO_DUPLICADO";
    public const string CodigoUsuarioNoEncontrado = "USUARIO_NO_ENCONTRADO";
    public const string CodigoUltimoAdministrador = "ULTIMO_ADMINISTRADOR";
    public const string CodigoLimiteSupervisores = "LIMITE_PLAN_SUPERVISORES";

    public async Task<PaginaDto<UsuarioDto>> ListarAsync(PaginacionDto paginacion, CancellationToken ct)
    {
        usuarioActual.RequerirRol(RolEmpresa.AdminEmpresa);
        await validadorPaginacion.ValidarOLanzarAsync(paginacion, ct);

        var consulta = db.Usuarios.AsNoTracking();
        var total = await consulta.CountAsync(ct);
        var items = await consulta
            .OrderBy(u => u.Nombre)
            .ThenBy(u => u.Id)
            .Skip(paginacion.Saltar)
            .Take(paginacion.TamanoPagina)
            .Select(u => new UsuarioDto(u.Id, u.EmpresaId, u.Nombre, u.Correo, u.Rol, u.Activo))
            .ToListAsync(ct);

        return new PaginaDto<UsuarioDto>(items, paginacion.Pagina, paginacion.TamanoPagina, total);
    }

    public async Task<UsuarioDto> CrearAsync(CrearUsuarioDto dto, CancellationToken ct)
    {
        var admin = usuarioActual.RequerirRol(RolEmpresa.AdminEmpresa);
        await validadorCrear.ValidarOLanzarAsync(dto, ct);

        var correo = NormalizadorIdentidad.Correo(dto.Email!);
        var usuario = new Usuario
        {
            EmpresaId = admin.EmpresaId,   // siempre la de la sesión, nunca del cuerpo
            Nombre = dto.Nombre!.Trim(),
            Correo = correo,
            Rol = dto.Rol!.Value,
            Activo = true,
            PasswordHash = hash.Hashear(dto.Password!),
        };

        await using var transaccion = await db.BeginTransactionAsync(ct);
        try
        {
            await bloqueo.BloquearAsync(admin.EmpresaId, ct);

            if (await db.Usuarios.AnyAsync(u => u.Correo == correo, ct))
            {
                throw CorreoDuplicado();
            }

            if (usuario.Rol == RolEmpresa.Supervisor)
            {
                await VerificarLimiteSupervisoresAsync(admin.EmpresaId, ct);
            }

            db.Usuarios.Add(usuario);
            await auditoria.RegistrarAsync(admin.EmpresaId, admin.UsuarioId, "CrearUsuario", "Usuario", usuario.Id,
                despues: new { usuario.Nombre, usuario.Correo, Rol = usuario.Rol.ToString(), usuario.Activo },
                cancellationToken: ct);
            await db.SaveChangesAsync(ct);
            await transaccion.CommitAsync(ct);
        }
        catch
        {
            db.LimpiarSeguimiento();
            throw;
        }

        return new UsuarioDto(usuario.Id, usuario.EmpresaId, usuario.Nombre, usuario.Correo, usuario.Rol, usuario.Activo);
    }

    public async Task ActualizarAsync(Guid usuarioId, ActualizarUsuarioDto dto, CancellationToken ct)
    {
        var admin = usuarioActual.RequerirRol(RolEmpresa.AdminEmpresa);
        await validadorActualizar.ValidarOLanzarAsync(dto, ct);

        await using var transaccion = await db.BeginTransactionAsync(ct);
        try
        {
            await bloqueo.BloquearAsync(admin.EmpresaId, ct);

            var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId, ct)
                ?? throw ExcepcionNegocio.NoEncontrado(CodigoUsuarioNoEncontrado, "No se encontró el usuario.");

            var antes = new { usuario.Nombre, usuario.Activo };
            var activo = dto.Activo!.Value;

            if (usuario.Activo && !activo && usuario.Rol == RolEmpresa.AdminEmpresa)
            {
                var otrosAdministradores = await db.Usuarios.CountAsync(
                    u => u.Id != usuario.Id && u.Rol == RolEmpresa.AdminEmpresa && u.Activo, ct);
                if (otrosAdministradores == 0)
                {
                    throw ExcepcionNegocio.Conflicto(CodigoUltimoAdministrador,
                        "No se puede desactivar al último AdminEmpresa activo de la empresa.");
                }
            }

            if (!usuario.Activo && activo && usuario.Rol == RolEmpresa.Supervisor)
            {
                await VerificarLimiteSupervisoresAsync(admin.EmpresaId, ct);
            }

            usuario.Nombre = dto.Nombre!.Trim();
            usuario.Activo = activo;

            await auditoria.RegistrarAsync(admin.EmpresaId, admin.UsuarioId, "ActualizarUsuario", "Usuario", usuario.Id,
                antes, new { usuario.Nombre, usuario.Activo }, ct);
            await db.SaveChangesAsync(ct);
            await transaccion.CommitAsync(ct);
        }
        catch
        {
            db.LimpiarSeguimiento();
            throw;
        }
    }

    /// Supervisores activos de la empresa más el que se agrega o reactiva.
    private async Task VerificarLimiteSupervisoresAsync(Guid empresaId, CancellationToken ct)
    {
        var activos = await db.Usuarios.CountAsync(u => u.Rol == RolEmpresa.Supervisor && u.Activo, ct);
        if (!await plan.AdmiteSupervisoresAsync(empresaId, activos + 1, ct))
        {
            throw ExcepcionNegocio.Prohibido(CodigoLimiteSupervisores, "El plan contratado no admite más supervisores activos.");
        }
    }

    private static ExcepcionNegocio CorreoDuplicado() =>
        ExcepcionNegocio.Conflicto(CodigoCorreoDuplicado, "Ya existe un usuario con ese email en la empresa.");
}
