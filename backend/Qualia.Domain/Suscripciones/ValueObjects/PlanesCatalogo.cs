namespace Qualia.Domain.Suscripciones;

/// <summary>
/// Identificadores y definiciones deterministas del catálogo inicial de planes
/// del MVP (fase1 §2.4). Se comparten entre el seed de EF Core y las pruebas.
/// </summary>
public static class PlanesCatalogo
{
    public const int RetencionDiasMvp = 365;

    public static readonly Guid BasicoId = new("10000000-0000-4000-8000-000000000001");
    public static readonly Guid ProId = new("10000000-0000-4000-8000-000000000002");
    public static readonly Guid EnterpriseId = new("10000000-0000-4000-8000-000000000003");
}
