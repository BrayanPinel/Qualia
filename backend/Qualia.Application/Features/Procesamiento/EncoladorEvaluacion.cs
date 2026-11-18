using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qualia.Application.Common.Errores;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Common.Resultados;
using Qualia.Domain.Entidades;
using Qualia.Domain.Enums;

namespace Qualia.Application.Features.Procesamiento;

/// <summary>
/// Encola la evaluación de una llamada en UNA transacción: Disponible → EnCola (UPDATE condicionado),
/// reserva de minutos y creación del Trabajo_procesamiento. Si algo falla, no queda nada a medias.
/// Lo usan el despachador automático y el inicio manual del supervisor.
/// En un reintento también acepta una llamada Fallida: reutiliza su trabajo y su consumo
/// (una fila de cada uno por llamada), de modo que no se duplica el cobro.
/// </summary>
public sealed class EncoladorEvaluacion(
    IQualiaDbContext db,
    IConsumoEvaluacion consumo,
    ILogger<EncoladorEvaluacion> logger)
{
    public async Task EncolarAsync(
        Guid empresaId, Guid llamadaId, decimal duracionSeg, CancellationToken ct, bool reintentarPerdida = false)
    {
        await using var transaccion = await db.BeginTransactionAsync(ct);

        // UPDATE ... WHERE estado = 'Disponible' (o 'Fallida' en un reintento): bloquea la fila;
        // si dos procesos lo intentan a la vez, el segundo encuentra 0 filas y recibe 409.
        var filas = await db.Llamadas
            .Where(l => l.Id == llamadaId
                        && (l.Estado == EstadoLlamada.Disponible || (reintentarPerdida && l.Estado == EstadoLlamada.Fallida)))
            .ExecuteUpdateAsync(s => s.SetProperty(l => l.Estado, EstadoLlamada.EnCola), ct);

        if (filas == 0)
        {
            throw EvaluacionYaIniciada();
        }

        // SUSCRIPCION_NO_ACTIVA (403) o CUOTA_AGOTADA (409) revierten todo.
        (await consumo.ReservarAsync(new ReservarConsumoRequest(empresaId, llamadaId, duracionSeg), ct))
            .ValorOLanzar();

        var trabajo = await db.TrabajosProcesamiento.FirstOrDefaultAsync(t => t.LlamadaId == llamadaId, ct);
        if (trabajo is null)
        {
            db.TrabajosProcesamiento.Add(TrabajoProcesamiento.Crear(empresaId, llamadaId));
        }
        else if (trabajo.Estado == EstadoTrabajo.Fallido)
        {
            trabajo.Reiniciar();
        }
        else
        {
            throw EvaluacionYaIniciada();
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            // Índice único (empresa_id, llamada_id): ya existe un trabajo para la llamada.
            logger.LogWarning(ex, "No se pudo crear el trabajo de la llamada {LlamadaId}", llamadaId);
            throw EvaluacionYaIniciada();
        }

        await transaccion.CommitAsync(ct);
    }

    public static ExcepcionNegocio EvaluacionYaIniciada() =>
        ExcepcionNegocio.Conflicto("EVALUACION_YA_INICIADA", "La evaluación de esta llamada ya fue iniciada.");
}
