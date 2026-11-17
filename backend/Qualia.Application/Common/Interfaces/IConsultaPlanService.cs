namespace Qualia.Application.Common.Interfaces;

/// <summary>
/// CONTRATO C → B (límites del plan contratado).
/// Es el SUBCONJUNTO que necesita el Rol B del contrato <c>IConsultaPlanService</c> que el Rol C
/// define en la rama feature/suscriptions (mismo nombre, espacio de nombres y firmas).
/// Al integrar el módulo de C, su versión completa reemplaza este archivo y la implementación
/// provisional se elimina; el código de B no cambia.
/// </summary>
public interface IConsultaPlanService
{
    /// <summary>Verifica si el plan admite el total resultante de supervisores.</summary>
    Task<bool> AdmiteSupervisoresAsync(
        Guid empresaId,
        int totalResultante,
        CancellationToken cancellationToken = default);

    /// <summary>Verifica si el plan admite el total resultante de rúbricas activas.</summary>
    Task<bool> AdmiteRubricasActivasAsync(
        Guid empresaId,
        int totalResultante,
        CancellationToken cancellationToken = default);
}
