namespace Qualia.Application.Features.Suscripciones.Consumo.ConsultarConsumo;

/// <summary>Parámetro de consulta GET /api/consumo?periodo=YYYY-MM.</summary>
public sealed record ConsultarConsumoQuery(string? Periodo);
