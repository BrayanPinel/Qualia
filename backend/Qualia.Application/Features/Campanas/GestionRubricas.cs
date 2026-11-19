using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qualia.Application.Common.Errores;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Common.Seguridad;
using Qualia.Application.Features.Campanas.Dtos;
using Qualia.Domain.Entidades;
using Qualia.Domain.Enums;

namespace Qualia.Application.Features.Campanas;

/// Rúbricas versionadas: GET/POST /api/empresas/{empresaId}/campanas/{id}/rubricas, PUT /api/empresas/{empresaId}/rubricas/{id}
/// y POST /api/empresas/{empresaId}/rubricas/{id}/publicar.
/// Cada POST crea una NUEVA versión en Borrador (versión = última de la campaña + 1).
/// Solo un Borrador se edita; una versión publicada es inmutable (RN-04).
/// Publicar exige pesos que sumen 100 y nombres sin duplicar (RN-05), retira la versión
/// Activa anterior de la campaña y respeta el límite de rúbricas activas del plan.
/// No hay borrado: las versiones usadas por llamadas se conservan (RN-07).
public sealed class GestionRubricas(
    IQualiaDbContext db,
    IUsuarioActual usuarioActual,
    IValidator<CrearRubricaDto> validador,
    IConsultaPlanService plan,
    IRegistroAuditoria auditoria,
    TimeProvider reloj,
    ILogger<GestionRubricas> logger)
{
    /// AdminEmpresa o Supervisor asignado a la campaña. Versiones de la más reciente a la más antigua.
    public async Task<IReadOnlyList<RubricaDto>> ListarPorCampanaAsync(Guid campanaId, CancellationToken ct)
    {
        var usuario = usuarioActual.RequerirRol(RolEmpresa.AdminEmpresa, RolEmpresa.Supervisor);
        await AsegurarCampanaExisteAsync(campanaId, ct);

        if (usuario.Rol == RolEmpresa.Supervisor
            && !await db.UsuariosCampana.AnyAsync(uc => uc.UsuarioId == usuario.UsuarioId && uc.CampanaId == campanaId, ct))
        {
            throw ExcepcionNegocio.Prohibido("CAMPANA_NO_ASIGNADA", "No está asignado a esta campaña.");
        }

        var rubricas = await db.Rubricas.AsNoTracking()
            .Include(r => r.Criterios)
            .Where(r => r.CampanaId == campanaId)
            .OrderByDescending(r => r.Version)
            .ToListAsync(ct);

        return rubricas.Select(MapearDto).ToList();
    }

    public async Task<RubricaDto> CrearAsync(Guid campanaId, CrearRubricaDto dto, CancellationToken ct)
    {
        var admin = RequerirAdmin();
        await validador.ValidarOLanzarAsync(dto, ct);
        await AsegurarCampanaExisteAsync(campanaId, ct);

        var ultimaVersion = await db.Rubricas
            .Where(r => r.CampanaId == campanaId)
            .MaxAsync(r => (int?)r.Version, ct) ?? 0;

        var rubrica = new Rubrica
        {
            EmpresaId = admin.EmpresaId,
            CampanaId = campanaId,
            Nombre = dto.Nombre!.Trim(),
            Version = ultimaVersion + 1,
            Estado = EstadoRubrica.Borrador,
            FechaCreacion = reloj.GetUtcNow().UtcDateTime,
        };
        foreach (var criterio in CrearCriterios(admin.EmpresaId, rubrica.Id, dto))
        {
            rubrica.Criterios.Add(criterio);
        }

        db.Rubricas.Add(rubrica);
        await auditoria.RegistrarAsync(admin.EmpresaId, admin.UsuarioId, "CrearRubrica", "Rubrica", rubrica.Id,
            despues: Resumen(rubrica), cancellationToken: ct);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            // UNIQUE (empresa_id, campana_id, version): otra versión se creó al mismo tiempo.
            logger.LogWarning(ex, "Conflicto al crear la versión {Version} de la campaña {CampanaId}", rubrica.Version, campanaId);
            throw ExcepcionNegocio.Conflicto("VERSION_EN_CONFLICTO",
                "Otra versión de la rúbrica se creó al mismo tiempo. Vuelva a intentarlo.");
        }

        return MapearDto(rubrica);
    }

    /// Reemplaza nombre y criterios de un Borrador (PUT con CrearRubricaDto).
    public async Task ActualizarAsync(Guid rubricaId, CrearRubricaDto dto, CancellationToken ct)
    {
        var admin = RequerirAdmin();
        await validador.ValidarOLanzarAsync(dto, ct);

        var rubrica = await BuscarConCriteriosAsync(rubricaId, ct);
        rubrica.AsegurarEditable();

        var antes = Resumen(rubrica);

        db.CriteriosRubrica.RemoveRange(rubrica.Criterios);
        rubrica.Criterios.Clear();
        rubrica.Nombre = dto.Nombre!.Trim();
        foreach (var criterio in CrearCriterios(rubrica.EmpresaId, rubrica.Id, dto))
        {
            rubrica.Criterios.Add(criterio);
        }

        await auditoria.RegistrarAsync(admin.EmpresaId, admin.UsuarioId, "ActualizarRubrica", "Rubrica", rubrica.Id,
            antes, Resumen(rubrica), ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<RubricaDto> PublicarAsync(Guid rubricaId, CancellationToken ct)
    {
        var admin = RequerirAdmin();
        var rubrica = await BuscarConCriteriosAsync(rubricaId, ct);
        rubrica.AsegurarEditable();

        var errores = Rubrica.ErroresDePublicacion(rubrica.Criterios);
        if (errores.Count > 0)
        {
            throw new ExcepcionNegocio(400, "RUBRICA_INVALIDA", "La rúbrica no cumple las reglas para publicarse.",
                new Dictionary<string, string[]> { ["criterios"] = [.. errores] });
        }

        // Límite de rúbricas activas: la publicación reemplaza la activa de la campaña, si existe.
        var activas = await db.Rubricas.CountAsync(r => r.Estado == EstadoRubrica.Activa, ct);
        var campanaTieneActiva = await db.Rubricas.AnyAsync(
            r => r.CampanaId == rubrica.CampanaId && r.Estado == EstadoRubrica.Activa, ct);
        var totalResultante = activas - (campanaTieneActiva ? 1 : 0) + 1;
        if (!await plan.AdmiteRubricasActivasAsync(admin.EmpresaId, totalResultante, ct))
        {
            throw ExcepcionNegocio.Prohibido("LIMITE_PLAN_RUBRICAS", "El plan contratado no admite más rúbricas activas.");
        }

        Guid? retiradaId;
        await using (var transaccion = await db.BeginTransactionAsync(ct))
        {
            // 1) Retira la versión activa anterior (si existe) antes de activar la nueva:
            //    el índice único parcial solo admite una rúbrica Activa por campaña.
            retiradaId = await db.Rubricas
                .Where(r => r.CampanaId == rubrica.CampanaId && r.Estado == EstadoRubrica.Activa)
                .Select(r => (Guid?)r.Id)
                .FirstOrDefaultAsync(ct);

            if (retiradaId is not null)
            {
                await db.Rubricas
                    .Where(r => r.Id == retiradaId && r.Estado == EstadoRubrica.Activa)
                    .ExecuteUpdateAsync(s => s.SetProperty(r => r.Estado, EstadoRubrica.Retirada), ct);
            }

            // 2) Publica el borrador (valida de nuevo en el dominio).
            rubrica.Publicar();

            await auditoria.RegistrarAsync(admin.EmpresaId, admin.UsuarioId, "PublicarRubrica", "Rubrica", rubrica.Id,
                new { Estado = EstadoRubrica.Borrador.ToString() },
                new { Estado = rubrica.Estado.ToString(), rubrica.Version, RubricaRetiradaId = retiradaId }, ct);

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                logger.LogWarning(ex, "Publicación concurrente en la campaña {CampanaId}", rubrica.CampanaId);
                throw ExcepcionNegocio.Conflicto("PUBLICACION_CONCURRENTE",
                    "Otra versión de esta campaña se publicó al mismo tiempo. Vuelva a intentarlo.");
            }

            await transaccion.CommitAsync(ct);
        }

        logger.LogInformation("Rúbrica {RubricaId} v{Version} publicada; retirada: {Retirada}", rubrica.Id, rubrica.Version, retiradaId);
        return MapearDto(rubrica);
    }

    internal static RubricaDto MapearDto(Rubrica r) => new(
        r.Id,
        r.EmpresaId,
        r.CampanaId,
        r.Nombre,
        r.Version,
        r.Estado,
        r.Criterios
            .OrderByDescending(c => c.Peso)
            .ThenBy(c => c.Nombre)
            .Select(c => new CriterioDto(c.Id, c.Nombre, c.Descripcion, c.Peso, c.EsCritico, c.UmbralCumplimiento))
            .ToList());

    private static IEnumerable<CriterioRubrica> CrearCriterios(Guid empresaId, Guid rubricaId, CrearRubricaDto dto) =>
        dto.Criterios!.Select(c => new CriterioRubrica
        {
            EmpresaId = empresaId,
            RubricaId = rubricaId,
            Nombre = c.Nombre!.Trim(),
            Descripcion = c.Descripcion!.Trim(),
            Peso = c.Peso!.Value,
            EsCritico = c.EsCritico!.Value,
            UmbralCumplimiento = c.UmbralCumplimiento!.Value,
        });

    private static object Resumen(Rubrica r) => new
    {
        r.Nombre,
        r.Version,
        Estado = r.Estado.ToString(),
        Criterios = r.Criterios.Select(c => new { c.Nombre, c.Peso, c.EsCritico, c.UmbralCumplimiento }).ToList(),
    };

    private UsuarioEmpresa RequerirAdmin() => usuarioActual.RequerirRol(RolEmpresa.AdminEmpresa);

    private async Task AsegurarCampanaExisteAsync(Guid campanaId, CancellationToken ct)
    {
        if (!await db.Campanas.AnyAsync(c => c.Id == campanaId, ct))
        {
            throw ExcepcionNegocio.NoEncontrado("CAMPANA_NO_ENCONTRADA", "La campaña no existe.");
        }
    }

    private async Task<Rubrica> BuscarConCriteriosAsync(Guid rubricaId, CancellationToken ct) =>
        await db.Rubricas.Include(r => r.Criterios).FirstOrDefaultAsync(r => r.Id == rubricaId, ct)
        ?? throw ExcepcionNegocio.NoEncontrado("RUBRICA_NO_ENCONTRADA", "La rúbrica no existe.");
}
