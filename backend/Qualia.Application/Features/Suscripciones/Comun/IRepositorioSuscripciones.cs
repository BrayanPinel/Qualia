using Qualia.Domain.Suscripciones;

namespace Qualia.Application.Features.Suscripciones.Comun;

/// <summary>
/// Puerto de persistencia del módulo (implementado en Infrastructure sobre el
/// <c>QualiaDbContext</c> del scope). Comparte la instancia del DbContext con el
/// resto de módulos de la misma petición, por lo que las operaciones de agregado
/// participan en la unidad de trabajo del llamador.
/// </summary>
public interface IRepositorioSuscripciones
{
    /// <summary>Planes activos con sus funciones, ordenados por precio (sin seguimiento).</summary>
    Task<IReadOnlyList<Plan>> ListarPlanesActivosAsync(CancellationToken cancellationToken = default);

    /// <summary>Plan con sus funciones (sin seguimiento), activo o no.</summary>
    Task<Plan?> ObtenerPlanAsync(Guid planId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Suscripción vigente (Prueba/Activa/EnMora) o, si no hay, la última registrada,
    /// con su plan y funciones (sin seguimiento). Respeta el filtro multiempresa.
    /// </summary>
    Task<Suscripcion?> ObtenerSuscripcionActualAsync(Guid empresaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Igual que <see cref="ObtenerSuscripcionActualAsync"/> pero sin depender del
    /// contexto HTTP (uso interno de otros módulos, PlatformAdmin o trabajador en segundo plano).
    /// <paramref name="empresaId"/> debe venir de una fuente confiable (JWT o trabajo en cola).
    /// </summary>
    Task<Suscripcion?> ObtenerSuscripcionActualSinContextoAsync(
        Guid empresaId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bloquea (<c>SELECT … FOR UPDATE</c>) y devuelve con seguimiento la suscripción
    /// actual de la empresa, con su plan. Requiere una transacción activa.
    /// </summary>
    Task<Suscripcion?> BloquearSuscripcionActualAsync(Guid empresaId, CancellationToken cancellationToken = default);

    /// <summary>Periodo de consumo de la empresa para el mes indicado, o <c>null</c>.</summary>
    Task<PeriodoConsumo?> ObtenerPeriodoAsync(
        Guid empresaId,
        PeriodoMensual mes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bloquea (<c>SELECT … FOR UPDATE</c>) y devuelve con seguimiento el periodo de consumo
    /// del mes indicado. Requiere una transacción activa.
    /// </summary>
    Task<PeriodoConsumo?> ObtenerPeriodoConBloqueoAsync(
        Guid empresaId,
        PeriodoMensual mes,
        CancellationToken cancellationToken = default);

    /// <summary>Sumas de minutos reservados y facturables de un periodo.</summary>
    Task<TotalesConsumo> ObtenerTotalesConsumoAsync(
        Guid empresaId,
        Guid periodoId,
        CancellationToken cancellationToken = default);

    /// <summary>Indica si el periodo posee al menos un registro de consumo (incluso Liberado).</summary>
    Task<bool> PeriodoTieneConsumosAsync(
        Guid empresaId,
        Guid periodoId,
        CancellationToken cancellationToken = default);

    /// <summary>Consumo registrado para la llamada indicada, con seguimiento, o <c>null</c>.</summary>
    Task<ConsumoLlamada?> ObtenerConsumoLlamadaAsync(
        Guid empresaId,
        Guid llamadaId,
        CancellationToken cancellationToken = default);

    /// <summary>Verifica si existe la empresa indicada (sin depender de filtro multiempresa).</summary>
    Task<bool> EmpresaExisteAsync(Guid empresaId, CancellationToken cancellationToken = default);

    /// <summary>Consulta paginada de proyección comercial de empresas para PlatformAdmin.</summary>
    Task<(IReadOnlyList<EmpresaComercialDto> Items, int Total)> ListarEmpresasComercialesAsync(
        int pagina,
        int tamanoPagina,
        PeriodoMensual mesActual,
        CancellationToken cancellationToken = default);

    void Agregar(Suscripcion suscripcion);

    void Agregar(PeriodoConsumo periodo);

    void Agregar(ConsumoLlamada consumo);

    void Agregar(AuditoriaPlataforma auditoria);

    /// <summary>
    /// Inicia una transacción en el DbContext compartido. Si ya existe una
    /// transacción ambiente, devuelve una transacción no-op que no la confirma.
    /// </summary>
    Task<ITransaccion> IniciarTransaccionAsync(CancellationToken cancellationToken = default);

    Task GuardarCambiosAsync(CancellationToken cancellationToken = default);
}

public sealed record TotalesConsumo(decimal MinutosReservados, decimal MinutosFacturables)
{
    public static TotalesConsumo Cero { get; } = new(0m, 0m);
}

/// <summary>Transacción de base de datos; se revierte al liberarse sin confirmar.</summary>
public interface ITransaccion : IAsyncDisposable
{
    Task ConfirmarAsync(CancellationToken cancellationToken = default);
}
