namespace Qualia.Application.Common.Interfaces;

/// Serializa, por empresa, las operaciones que dependen de un conteo (último AdminEmpresa activo,
/// límite de supervisores del plan). Bloquea la fila de la empresa hasta el fin de la transacción
/// del llamador: dos peticiones concurrentes de la misma empresa se ejecutan una detrás de otra.
public interface IBloqueoEmpresa
{
    /// Requiere una transacción abierta en el contexto del scope.
    Task BloquearAsync(Guid empresaId, CancellationToken cancellationToken = default);
}
