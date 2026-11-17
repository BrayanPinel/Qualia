namespace Qualia.Application.Features.Suscripciones.Consumo.ConsultarConsumo;

/// <summary>Parámetro de consulta <c>GET /api/consumo?periodo=YYYY-MM</c> (fase1 §7.2).</summary>
public sealed record ConsultarConsumoQuery(string? Periodo);
