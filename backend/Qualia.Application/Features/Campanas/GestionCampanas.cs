using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qualia.Application.Common.Errores;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Common.Models;
using Qualia.Application.Common.Seguridad;
using Qualia.Application.Features.Campanas.Dtos;
using Qualia.Domain.Entidades;
using Qualia.Domain.Enums;

namespace Qualia.Application.Features.Campanas;

/// Casos de uso de campañas y asignaciones:
/// GET/POST/PUT /api/campanas y GET/PUT/DELETE /api/campanas/{id}/usuarios[/{usuarioId}].
/// El filtro global ya limita a la empresa del token; aquí se aplican el rol y el alcance.
public sealed class GestionCampanas(
    IQualiaDbContext db,
    IUsuarioActual usuarioActual,
    IValidator<GuardarCampanaDto> validadorCampana,
    IValidator<PaginacionDto> validadorPaginacion,
    IConsultaPlanService plan,
    IRegistroAuditoria auditoria,
    ILogger<GestionCampanas> logger)
{
    /// AdminEmpresa y Auditor: todas las campañas de su empresa. Supervisor: las asignadas.
    /// Agente: las asignadas o aquellas donde conserva llamadas propias.
    public async Task<PaginaDto<CampanaDto>> ListarAsync(PaginacionDto paginacion, CancellationToken ct)
    {
        var usuario = usuarioActual.RequerirRol(RolEmpresa.AdminEmpresa, RolEmpresa.Supervisor, RolEmpresa.Agente, RolEmpresa.Auditor);
        await validadorPaginacion.ValidarOLanzarAsync(paginacion, ct);

        var consulta = db.Campanas.AsNoTracking();
        consulta = usuario.Rol switch
        {
            RolEmpresa.Supervisor => consulta.Where(c => db.UsuariosCampana.Any(uc =>
                uc.UsuarioId == usuario.UsuarioId && uc.CampanaId == c.Id)),
            RolEmpresa.Agente => consulta.Where(c =>
                db.UsuariosCampana.Any(uc => uc.UsuarioId == usuario.UsuarioId && uc.CampanaId == c.Id)
                || db.Llamadas.Any(l => l.AgenteId == usuario.UsuarioId && l.Rubrica!.CampanaId == c.Id)),
            _ => consulta,
        };

        var total = await consulta.CountAsync(ct);
        var items = await consulta
            .OrderBy(c => c.Nombre)
            .ThenBy(c => c.Id)
            .Skip(paginacion.Saltar)
            .Take(paginacion.TamanoPagina)
            .Select(c => new CampanaDto(
                c.Id,
                c.EmpresaId,
                c.Nombre,
                c.Activo,
                db.Rubricas
                    .Where(r => r.CampanaId == c.Id && r.Estado == EstadoRubrica.Activa)
                    .Select(r => (Guid?)r.Id)
                    .FirstOrDefault()))
            .ToListAsync(ct);

        return new PaginaDto<CampanaDto>(items, paginacion.Pagina, paginacion.TamanoPagina, total);
    }

    public async Task<CampanaDto> CrearAsync(GuardarCampanaDto dto, CancellationToken ct)
    {
        var admin = RequerirAdmin();
        await validadorCampana.ValidarOLanzarAsync(dto, ct);

        var campana = new Campana
        {
            EmpresaId = admin.EmpresaId,
            Nombre = dto.Nombre!.Trim(),
            Activo = dto.Activa!.Value,
        };
        db.Campanas.Add(campana);

        await auditoria.RegistrarAsync(admin.EmpresaId, admin.UsuarioId, "CrearCampana", "Campana", campana.Id,
            despues: new { campana.Nombre, Activa = campana.Activo }, cancellationToken: ct);
        await db.SaveChangesAsync(ct);

        return new CampanaDto(campana.Id, campana.EmpresaId, campana.Nombre, campana.Activo, null);
    }

    public async Task ActualizarAsync(Guid campanaId, GuardarCampanaDto dto, CancellationToken ct)
    {
        var admin = RequerirAdmin();
        await validadorCampana.ValidarOLanzarAsync(dto, ct);

        var campana = await BuscarCampanaAsync(campanaId, ct);
        var antes = new { campana.Nombre, Activa = campana.Activo };

        campana.Nombre = dto.Nombre!.Trim();
        campana.Activo = dto.Activa!.Value;

        await auditoria.RegistrarAsync(admin.EmpresaId, admin.UsuarioId, "ActualizarCampana", "Campana", campana.Id,
            antes, new { campana.Nombre, Activa = campana.Activo }, ct);
        await db.SaveChangesAsync(ct);
    }

    /// Usuarios asignados a la campaña (solo AdminEmpresa).
    public async Task<PaginaDto<UsuarioDto>> ListarUsuariosAsync(Guid campanaId, PaginacionDto paginacion, CancellationToken ct)
    {
        RequerirAdmin();
        await validadorPaginacion.ValidarOLanzarAsync(paginacion, ct);
        await AsegurarCampanaExisteAsync(campanaId, ct);

        var consulta = db.Usuarios.AsNoTracking()
            .Where(u => db.UsuariosCampana.Any(uc => uc.CampanaId == campanaId && uc.UsuarioId == u.Id));

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

    /// Asignación idempotente. Solo Agente o Supervisor activos de la misma empresa (RN-03).
    /// Para supervisores se consulta el límite del plan con el contrato del Rol C: cuenta los
    /// supervisores que quedarían con al menos una campaña asignada.
    public async Task AsignarUsuarioAsync(Guid campanaId, Guid usuarioId, CancellationToken ct)
    {
        var admin = RequerirAdmin();
        await AsegurarCampanaExisteAsync(campanaId, ct);
        var usuario = await BuscarUsuarioAsync(usuarioId, ct);

        if (!UsuarioCampana.RolAsignable(usuario.Rol))
        {
            throw ExcepcionNegocio.Conflicto("ROL_NO_ASIGNABLE", "Solo se pueden asignar usuarios con rol Agente o Supervisor.");
        }

        if (!usuario.Activo)
        {
            throw ExcepcionNegocio.Conflicto("USUARIO_INACTIVO", "No se puede asignar un usuario inactivo.");
        }

        if (await db.UsuariosCampana.AnyAsync(uc => uc.CampanaId == campanaId && uc.UsuarioId == usuarioId, ct))
        {
            return; // Idempotente: ya estaba asignado.
        }

        if (usuario.Rol == RolEmpresa.Supervisor)
        {
            await VerificarLimiteSupervisoresAsync(admin.EmpresaId, usuarioId, ct);
        }

        db.UsuariosCampana.Add(new UsuarioCampana { EmpresaId = admin.EmpresaId, UsuarioId = usuarioId, CampanaId = campanaId });
        await auditoria.RegistrarAsync(admin.EmpresaId, admin.UsuarioId, "AsignarUsuarioCampana", "Campana", campanaId,
            despues: new { UsuarioId = usuarioId, Rol = usuario.Rol.ToString() }, cancellationToken: ct);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            // Otra petición simultánea ya creó la misma asignación (PK compuesta): el resultado es el mismo.
            logger.LogInformation(ex, "Asignación concurrente de {UsuarioId} a la campaña {CampanaId}", usuarioId, campanaId);
            db.LimpiarSeguimiento();
            if (!await db.UsuariosCampana.AnyAsync(uc => uc.CampanaId == campanaId && uc.UsuarioId == usuarioId, ct))
            {
                throw;
            }
        }
    }

    /// Revoca la asignación. Idempotente: si no existía, no hace nada.
    public async Task RevocarUsuarioAsync(Guid campanaId, Guid usuarioId, CancellationToken ct)
    {
        var admin = RequerirAdmin();
        await AsegurarCampanaExisteAsync(campanaId, ct);
        await BuscarUsuarioAsync(usuarioId, ct);

        var asignacion = await db.UsuariosCampana
            .FirstOrDefaultAsync(uc => uc.CampanaId == campanaId && uc.UsuarioId == usuarioId, ct);
        if (asignacion is null)
        {
            return;
        }

        db.UsuariosCampana.Remove(asignacion);
        await auditoria.RegistrarAsync(admin.EmpresaId, admin.UsuarioId, "RevocarUsuarioCampana", "Campana", campanaId,
            antes: new { UsuarioId = usuarioId }, cancellationToken: ct);
        await db.SaveChangesAsync(ct);
    }

    private async Task VerificarLimiteSupervisoresAsync(Guid empresaId, Guid supervisorId, CancellationToken ct)
    {
        var supervisoresAsignados = await db.UsuariosCampana
            .Where(uc => db.Usuarios.Any(u => u.Id == uc.UsuarioId && u.Rol == RolEmpresa.Supervisor))
            .Select(uc => uc.UsuarioId)
            .Distinct()
            .ToListAsync(ct);

        var totalResultante = supervisoresAsignados.Count + (supervisoresAsignados.Contains(supervisorId) ? 0 : 1);
        if (!await plan.AdmiteSupervisoresAsync(empresaId, totalResultante, ct))
        {
            throw ExcepcionNegocio.Prohibido("LIMITE_PLAN_SUPERVISORES",
                "El plan contratado no admite más supervisores asignados.");
        }
    }

    private UsuarioEmpresa RequerirAdmin() => usuarioActual.RequerirRol(RolEmpresa.AdminEmpresa);

    private async Task<Campana> BuscarCampanaAsync(Guid campanaId, CancellationToken ct) =>
        await db.Campanas.FirstOrDefaultAsync(c => c.Id == campanaId, ct)
        ?? throw ExcepcionNegocio.NoEncontrado("CAMPANA_NO_ENCONTRADA", "La campaña no existe.");

    private async Task AsegurarCampanaExisteAsync(Guid campanaId, CancellationToken ct)
    {
        if (!await db.Campanas.AnyAsync(c => c.Id == campanaId, ct))
        {
            throw ExcepcionNegocio.NoEncontrado("CAMPANA_NO_ENCONTRADA", "La campaña no existe.");
        }
    }

    /// Un usuario de otra empresa queda oculto por el filtro global: 404, igual que uno inexistente.
    private async Task<Usuario> BuscarUsuarioAsync(Guid usuarioId, CancellationToken ct) =>
        await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == usuarioId, ct)
        ?? throw ExcepcionNegocio.NoEncontrado("USUARIO_NO_ENCONTRADO", "El usuario no existe.");
}
