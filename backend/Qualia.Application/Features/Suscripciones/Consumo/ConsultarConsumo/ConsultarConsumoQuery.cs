namespace Qualia.Application.Features.Suscripciones.Consumo.ConsultarConsumo;

/// Parámetro de consulta GET /api/consumo?periodo=YYYY-MM.
public sealed record ConsultarConsumoQuery(string? Periodo);
