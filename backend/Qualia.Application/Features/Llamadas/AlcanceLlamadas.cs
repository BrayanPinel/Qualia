using Microsoft.EntityFrameworkCore;
using Qualia.Application.Comun.Errores;
using Qualia.Application.Comun.Persistencia;
using Qualia.Application.Comun.Seguridad;
using Qualia.Domain.Entidades;
using Qualia.Domain.Enums;

namespace Qualia.Application.Features.Llamadas;

/// <summary>
/// Autorización por recurso de las llamadas. El filtro global ya limita
/// a la empresa del token; aquí se agrega el alcance por rol:
/// <list type="bullet">
/// <item>AdminEmpresa: todas las llamadas de su empresa.</item>
/// <item>Supervisor: llamadas de campañas a las que está asignado (campaña obtenida vía rúbrica).</item>
/// <item>Agente: solo sus llamadas.</item>
/// <item>Auditor / PlatformAdmin: sin acceso al contenido de llamadas (403).</item>
/// </list>
/// </summary>
public sealed class AlcanceLlamadas(IQualiaDbContext db, IUsuarioActual usuarioActual)
{
    public UsuarioEmpresa RequerirLector() =>
        usuarioActual.RequerirRol(RolEmpresa.AdminEmpresa, RolEmpresa.Supervisor, RolEmpresa.Agente);

    public IQueryable<Llamada> Filtrar(IQueryable<Llamada> consulta, UsuarioEmpresa usuario) => usuario.Rol switch
    {
        RolEmpresa.AdminEmpresa => consulta,
        RolEmpresa.Supervisor => consulta.Where(l => db.UsuariosCampana.Any(uc =>
            uc.UsuarioId == usuario.UsuarioId && uc.CampanaId == l.Rubrica!.CampanaId)),
        RolEmpresa.Agente => consulta.Where(l => l.AgenteId == usuario.UsuarioId),
        _ => consulta.Where(_ => false),
    };

    /// <summary>
    /// 404 si la llamada no existe o es de otra empresa (el filtro global la oculta);
    /// 403 si existe en la empresa pero el usuario no tiene alcance sobre ella.
    /// </summary>
    public async Task<UsuarioEmpresa> VerificarAccesoAsync(Guid llamadaId, CancellationToken ct)
    {
        var usuario = RequerirLector();

        if (!await db.Llamadas.AnyAsync(l => l.Id == llamadaId, ct))
        {
            throw ExcepcionNegocio.NoEncontrado("LLAMADA_NO_ENCONTRADA", "La llamada no existe.");
        }

        if (!await Filtrar(db.Llamadas, usuario).AnyAsync(l => l.Id == llamadaId, ct))
        {
            throw ExcepcionNegocio.Prohibido("LLAMADA_FUERA_DE_ALCANCE", "No tiene acceso a esta llamada.");
        }

        return usuario;
    }
}
