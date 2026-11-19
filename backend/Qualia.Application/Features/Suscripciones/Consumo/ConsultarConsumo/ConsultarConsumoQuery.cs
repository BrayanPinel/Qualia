namespace Qualia.Application.Features.Suscripciones.Consumo.ConsultarConsumo;

/// Parámetro de consulta GET /api/empresas/{empresaId}/consumo?periodo=YYYY-MM.
public sealed record ConsultarConsumoQuery(string? Periodo);
