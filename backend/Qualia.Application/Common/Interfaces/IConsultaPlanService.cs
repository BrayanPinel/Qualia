using Qualia.Application.Common.Resultados;
using Qualia.Application.Features.Suscripciones.Comun;

namespace Qualia.Application.Common.Interfaces;

/// <summary>
/// Contrato compartido (expuesto por Rol C, consumido por Roles A, B y D).
/// Consulta el plan contratado vigente sin exponer entidades internas de suscripción.
/// </summary>
public interface IConsultaPlanService
{
    /// <summary>Indica si el plan vigente de la empresa habilita la función indicada.</summary>
    Task<bool> TieneFuncionAsync(
        Guid empresaId,
        string codigoFuncion,
        CancellationToken cancellationToken = default);

    /// <summary>Verifica si el plan admite el total resultante de supervisores activos.</summary>
    Task<bool> AdmiteSupervisoresAsync(
        Guid empresaId,
        int totalResultante,
        CancellationToken cancellationToken = default);

    /// <summary>Verifica si el plan admite el total resultante de rúbricas activas.</summary>
    Task<bool> AdmiteRubricasActivasAsync(
        Guid empresaId,
        int totalResultante,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Valida si la suscripción vigente de la empresa permite iniciar nuevas evaluaciones
    /// (estado Prueba o Activa y dentro de vigencia).
    /// </summary>
    Task<Resultado<SuscripcionDto>> VerificarSuscripcionOperativaAsync(
        Guid empresaId,
        CancellationToken cancellationToken = default);
}
