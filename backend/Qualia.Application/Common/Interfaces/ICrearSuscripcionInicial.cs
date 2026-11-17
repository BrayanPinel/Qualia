using Qualia.Application.Common.Resultados;
using Qualia.Application.Features.Suscripciones.Comun;

namespace Qualia.Application.Common.Interfaces;

/// <summary>
/// Contrato compartido (expuesto por Rol C, consumido por Rol A en POST /api/empresas).
/// Crea la suscripción de prueba y el periodo inicial dentro de la misma unidad de
/// trabajo transaccional iniciada por el llamador (HU-01).
/// </summary>
public interface ICrearSuscripcionInicial
{
    Task<Resultado<SuscripcionDto>> CrearInicialAsync(
        Guid empresaId,
        Guid planId,
        DateTime fechaRegistroUtc,
        CancellationToken cancellationToken = default);
}
