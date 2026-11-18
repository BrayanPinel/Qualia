namespace Qualia.Application.Common.Interfaces;

/// <summary>
/// Serializa, por empresa, las operaciones que dependen de un conteo (último AdminEmpresa activo,
/// límite de supervisores del plan). Bloquea la fila de la empresa hasta el fin de la transacción
/// del llamador: dos peticiones concurrentes de la misma empresa se ejecutan una detrás de otra.
/// </summary>
public interface IBloqueoEmpresa
{
    /// <summary>Requiere una transacción abierta en el contexto del scope.</summary>
    Task BloquearAsync(Guid empresaId, CancellationToken cancellationToken = default);
}
