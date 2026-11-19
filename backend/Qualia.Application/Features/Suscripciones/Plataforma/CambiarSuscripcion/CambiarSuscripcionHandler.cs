using System.Text.Json;
using FluentValidation;
using Qualia.Application.Common.Resultados;
using Qualia.Application.Features.Suscripciones.Comun;
using Qualia.Domain.Suscripciones;

namespace Qualia.Application.Features.Suscripciones.Plataforma.CambiarSuscripcion;

/// Caso de uso PUT /api/plataforma/empresas/{empresaId}/suscripcion.
/// Aplica la regla CAMBIO_PLAN_PERIODO_EN_USO y registra AuditoriaPlataforma.
public sealed class CambiarSuscripcionHandler
{
    private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);

    private readonly IRepositorioSuscripciones _repositorio;
    private readonly IValidator<CambiarSuscripcionCommand> _validador;
    private readonly TimeProvider _timeProvider;

    public CambiarSuscripcionHandler(
        IRepositorioSuscripciones repositorio,
        IValidator<CambiarSuscripcionCommand> validador,
        TimeProvider timeProvider)
    {
        _repositorio = repositorio;
        _validador = validador;
        _timeProvider = timeProvider;
    }

    public async Task<Resultado<SuscripcionDto>> EjecutarAsync(
        CambiarSuscripcionCommand command,
        CancellationToken cancellationToken = default)
    {
        var errorValidacion = await _validador.ValidarAsync(command, cancellationToken);
        if (errorValidacion is not null)
        {
            return errorValidacion;
        }

        if (!await _repositorio.EmpresaExisteAsync(command.EmpresaId, cancellationToken))
        {
            return ErroresSuscripcion.EmpresaNoEncontrada;
        }

        var plan = await _repositorio.ObtenerPlanAsync(command.Datos.PlanId, cancellationToken);
        if (plan is null || !plan.Activo)
        {
            return ErroresSuscripcion.PlanNoDisponible;
        }

        var estadoNuevo = Enum.Parse<EstadoSuscripcion>(command.Datos.Estado.Trim(), ignoreCase: false);
        var ahoraUtc = _timeProvider.GetUtcNow().UtcDateTime;

        await using var transaccion = await _repositorio.IniciarTransaccionAsync(cancellationToken);

        var suscripcionActual = await _repositorio.BloquearSuscripcionActualAsync(
            command.EmpresaId,
            cancellationToken);

        var antesJson = suscripcionActual is null
            ? null
            : SerializarSnapshot(suscripcionActual, motivo: null);

        var mesActual = PeriodoMensual.DesdeFecha(ahoraUtc);
        var periodoActual = await _repositorio.ObtenerPeriodoConBloqueoAsync(
            command.EmpresaId,
            mesActual,
            cancellationToken);

        Suscripcion suscripcionResultante;
        var cambiaDePlan = suscripcionActual is null || suscripcionActual.PlanId != plan.Id;

        if (cambiaDePlan)
        {
            if (periodoActual is not null &&
                await _repositorio.PeriodoTieneConsumosAsync(command.EmpresaId, periodoActual.Id, cancellationToken))
            {
                return ErroresSuscripcion.CambioPlanPeriodoEnUso;
            }

            if (command.Datos.FechaVencimiento <= ahoraUtc)
            {
                return ErrorAplicacion.Validacion(new Dictionary<string, string[]>
                {
                    ["fechaVencimiento"] = ["La fecha de vencimiento debe ser posterior al instante actual UTC."],
                });
            }

            suscripcionActual?.CerrarPorCambioDePlan();

            suscripcionResultante = Suscripcion.CrearPorCambioDePlan(
                command.EmpresaId,
                plan,
                estadoNuevo,
                ahoraUtc,
                command.Datos.FechaVencimiento,
                command.Datos.PermiteExcedente);

            _repositorio.Agregar(suscripcionResultante);

            if (periodoActual is not null)
            {
                periodoActual.ReemplazarPlanEnPeriodoVacio(suscripcionResultante, plan.MinutosIncluidos);
            }
            else if (estadoNuevo is EstadoSuscripcion.Prueba or EstadoSuscripcion.Activa)
            {
                periodoActual = PeriodoConsumo.Abrir(suscripcionResultante, mesActual, plan.MinutosIncluidos);
                _repositorio.Agregar(periodoActual);
            }
        }
        else
        {
            if (command.Datos.FechaVencimiento <= suscripcionActual!.FechaInicio)
            {
                return ErrorAplicacion.Validacion(new Dictionary<string, string[]>
                {
                    ["fechaVencimiento"] = ["La fecha de vencimiento debe ser posterior al inicio de la suscripción."],
                });
            }

            suscripcionActual.ActualizarCondicionesComerciales(
                estadoNuevo,
                command.Datos.FechaVencimiento,
                command.Datos.PermiteExcedente);

            suscripcionResultante = suscripcionActual;
        }

        var despuesJson = SerializarSnapshot(suscripcionResultante, command.Datos.Motivo.Trim());
        var auditoria = AuditoriaPlataforma.Registrar(
            command.AdministradorId,
            command.EmpresaId,
            cambiaDePlan ? "CAMBIAR_PLAN_SUSCRIPCION" : "ACTUALIZAR_SUSCRIPCION",
            antesJson,
            despuesJson,
            ahoraUtc);

        _repositorio.Agregar(auditoria);

        await _repositorio.GuardarCambiosAsync(cancellationToken);
        await transaccion.ConfirmarAsync(cancellationToken);

        return suscripcionResultante.ASuscripcionDto(periodoActual);
    }

    private static string SerializarSnapshot(Suscripcion suscripcion, string? motivo) =>
        JsonSerializer.Serialize(
            new
            {
                suscripcion.Id,
                suscripcion.PlanId,
                Estado = suscripcion.Estado.ToString(),
                suscripcion.FechaInicio,
                suscripcion.FechaVencimiento,
                suscripcion.PermiteExcedente,
                Motivo = motivo,
            },
            OpcionesJson);
}
