using Qualia.Domain.Suscripciones;

namespace Qualia.Application.Features.Suscripciones.Comun;

/// Puerto de persistencia del módulo (implementado en Infrastructure sobre el
/// QualiaDbContext del scope). Comparte la instancia del DbContext con el
/// resto de módulos de la misma petición, por lo que las operaciones de agregado
/// participan en la unidad de trabajo del llamador.
public interface IRepositorioSuscripciones
{
    /// Planes activos con sus funciones, ordenados por precio (sin seguimiento).
    Task<IReadOnlyList<Plan>> ListarPlanesActivosAsync(CancellationToken cancellationToken = default);

    /// Plan con sus funciones (sin seguimiento), activo o no.
    Task<Plan?> ObtenerPlanAsync(Guid planId, CancellationToken cancellationToken = default);

    /// Suscripción vigente (Prueba/Activa/EnMora) o, si no hay, la última registrada,
    /// con su plan y funciones (sin seguimiento). Respeta el filtro multiempresa.
    Task<Suscripcion?> ObtenerSuscripcionActualAsync(Guid empresaId, CancellationToken cancellationToken = default);

    /// Igual que ObtenerSuscripcionActualAsync pero sin depender del
    /// contexto HTTP (uso interno de otros módulos, PlatformAdmin o trabajador en segundo plano).
    /// empresaId debe venir de una fuente confiable (JWT o trabajo en cola).
    Task<Suscripcion?> ObtenerSuscripcionActualSinContextoAsync(
        Guid empresaId,
        CancellationToken cancellationToken = default);

    /// Bloquea (SELECT … FOR UPDATE) y devuelve con seguimiento la suscripción
    /// actual de la empresa, con su plan. Requiere una transacción activa.
    Task<Suscripcion?> BloquearSuscripcionActualAsync(Guid empresaId, CancellationToken cancellationToken = default);

    /// Periodo de consumo de la empresa para el mes indicado, o null.
    Task<PeriodoConsumo?> ObtenerPeriodoAsync(
        Guid empresaId,
        PeriodoMensual mes,
        CancellationToken cancellationToken = default);

    /// Bloquea (SELECT … FOR UPDATE) y devuelve con seguimiento el periodo de consumo
    /// del mes indicado. Requiere una transacción activa.
    Task<PeriodoConsumo?> ObtenerPeriodoConBloqueoAsync(
        Guid empresaId,
        PeriodoMensual mes,
        CancellationToken cancellationToken = default);

    /// Sumas de minutos reservados y facturables de un periodo.
    Task<TotalesConsumo> ObtenerTotalesConsumoAsync(
        Guid empresaId,
        Guid periodoId,
        CancellationToken cancellationToken = default);

    /// Indica si el periodo posee al menos un registro de consumo (incluso Liberado).
    Task<bool> PeriodoTieneConsumosAsync(
        Guid empresaId,
        Guid periodoId,
        CancellationToken cancellationToken = default);

    /// Consumo registrado para la llamada indicada, con seguimiento, o null.
    Task<ConsumoLlamada?> ObtenerConsumoLlamadaAsync(
        Guid empresaId,
        Guid llamadaId,
        CancellationToken cancellationToken = default);

    /// Verifica si existe la empresa indicada (sin depender de filtro multiempresa).
    Task<bool> EmpresaExisteAsync(Guid empresaId, CancellationToken cancellationToken = default);

    /// Consulta paginada de proyección comercial de empresas para PlatformAdmin.
    Task<(IReadOnlyList<EmpresaComercialDto> Items, int Total)> ListarEmpresasComercialesAsync(
        int pagina,
        int tamanoPagina,
        PeriodoMensual mesActual,
        CancellationToken cancellationToken = default);

    void Agregar(Suscripcion suscripcion);

    void Agregar(PeriodoConsumo periodo);

    void Agregar(ConsumoLlamada consumo);

    void Agregar(AuditoriaPlataforma auditoria);

    /// Inicia una transacción en el DbContext compartido. Si ya existe una
    /// transacción ambiente, devuelve una transacción no-op que no la confirma.
    Task<ITransaccion> IniciarTransaccionAsync(CancellationToken cancellationToken = default);

    Task GuardarCambiosAsync(CancellationToken cancellationToken = default);
}

public sealed record TotalesConsumo(decimal MinutosReservados, decimal MinutosFacturables)
{
    public static TotalesConsumo Cero { get; } = new(0m, 0m);
}

/// Transacción de base de datos; se revierte al liberarse sin confirmar.
public interface ITransaccion : IAsyncDisposable
{
    Task ConfirmarAsync(CancellationToken cancellationToken = default);
}
