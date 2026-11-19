using Microsoft.EntityFrameworkCore;
using Qualia.Application.Common.Interfaces;

namespace Qualia.Application.Features.Revision;

/// Implementación del Rol B del contrato B → D (y C) IConsultaRevisionVigente:
/// el resultado humano vigente es el del ÚLTIMO AJUSTE COMPLETO (la revisión más reciente que tiene
/// criterios revisados). Las confirmaciones posteriores no lo borran.
public sealed class ConsultaRevisionVigente(IQualiaDbContext db) : IConsultaRevisionVigente
{
    public async Task<AjusteVigente?> ObtenerAjusteVigenteAsync(Guid empresaId, Guid evaluacionId, CancellationToken cancellationToken)
    {
        var revisionId = await db.RevisionesHumanas.AsNoTracking()
            .Where(r => r.EmpresaId == empresaId && r.EvaluacionId == evaluacionId && r.Criterios.Any())
            .OrderByDescending(r => r.FechaRevision)
            .ThenByDescending(r => r.Id)
            .Select(r => (Guid?)r.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (revisionId is null)
        {
            return null;
        }

        var criterios = await db.CriteriosRevisados.AsNoTracking()
            .Include(c => c.Evidencias)
            .Where(c => c.EmpresaId == empresaId && c.RevisionId == revisionId)
            .ToListAsync(cancellationToken);

        return new AjusteVigente(criterios.ToDictionary(
            c => c.DetalleId,
            c => new CriterioAjustado(
                c.PuntajeAjustado,
                c.Observacion,
                c.Evidencias.Select(e => new EvidenciaAjustada(e.SegmentoId, e.CitaTextual)).ToList())));
    }
}
