using Microsoft.EntityFrameworkCore;
using Qualia.Application.Common.Interfaces;

namespace Qualia.Infrastructure.Persistence;

/// <summary>
/// <see cref="IBloqueoEmpresa"/> con SELECT … FOR NO KEY UPDATE sobre la fila de la empresa.
/// NO KEY UPDATE (y no FOR UPDATE) para no bloquear las inserciones de otras tablas que solo
/// verifican la FK hacia empresa: solo se esperan entre sí las operaciones que usan este bloqueo.
/// </summary>
public sealed class BloqueoEmpresa(QualiaDbContext db) : IBloqueoEmpresa
{
    public async Task BloquearAsync(Guid empresaId, CancellationToken cancellationToken = default)
    {
        if (db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("El bloqueo por empresa requiere una transacción abierta.");
        }

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM qualia.empresa WHERE id = {empresaId} FOR NO KEY UPDATE", cancellationToken);
    }
}
