using Qualia.Application.Common.Resultados;
using Qualia.Application.Features.Suscripciones.Comun;

namespace Qualia.Application.Common.Interfaces;

/// Contrato compartido (expuesto por Rol C, consumido por Roles A, B y D).
/// Consulta el plan contratado vigente sin exponer entidades internas de suscripción.
public interface IConsultaPlanService
{
    /// Indica si el plan vigente de la empresa habilita la función indicada.
    Task<bool> TieneFuncionAsync(
        Guid empresaId,
        string codigoFuncion,
        CancellationToken cancellationToken = default);

    /// Verifica si el plan admite el total resultante de supervisores activos.
    Task<bool> AdmiteSupervisoresAsync(
        Guid empresaId,
        int totalResultante,
        CancellationToken cancellationToken = default);

    /// Verifica si el plan admite el total resultante de rúbricas activas.
    Task<bool> AdmiteRubricasActivasAsync(
        Guid empresaId,
        int totalResultante,
        CancellationToken cancellationToken = default);

    /// Valida si la suscripción vigente de la empresa permite iniciar nuevas evaluaciones
    /// (estado Prueba o Activa y dentro de vigencia).
    Task<Resultado<SuscripcionDto>> VerificarSuscripcionOperativaAsync(
        Guid empresaId,
        CancellationToken cancellationToken = default);
}
