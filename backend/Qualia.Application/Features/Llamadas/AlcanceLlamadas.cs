using Microsoft.EntityFrameworkCore;
using Qualia.Application.Common.Errores;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Common.Seguridad;
using Qualia.Domain.Entidades;
using Qualia.Domain.Enums;

namespace Qualia.Application.Features.Llamadas;

/// Autorización por recurso de las llamadas. El filtro global ya limita
/// a la empresa del token; aquí se agrega el alcance por rol:
/// AdminEmpresa: todas las llamadas de su empresa.
/// Supervisor: llamadas de campañas a las que está asignado (campaña obtenida vía rúbrica).
/// Agente: solo sus llamadas.
/// Auditor / PlatformAdmin: sin acceso al contenido de llamadas (403).
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

    /// 404 si la llamada no existe o es de otra empresa (el filtro global la oculta);
    /// 403 si existe en la empresa pero el usuario no tiene alcance sobre ella.
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
