using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Common.Errores;
using Qualia.Application.Common.Resultados;
using Qualia.Application.Common.Seguridad;
using Qualia.Application.Features.Llamadas.Dtos;
using Qualia.Application.Features.Procesamiento;
using Qualia.Domain.Entidades;
using Qualia.Domain.Enums;

namespace Qualia.Application.Features.Llamadas;

/// <summary>
/// POST /api/llamadas/{id}/evaluacion — el Supervisor asignado encola la evaluación.
/// En UNA transacción: Disponible → EnCola (UPDATE condicionado), reserva de minutos.
/// y creación del Trabajo_procesamiento. Si algo falla, no queda nada a medias.
/// </summary>

public sealed class IniciarEvaluacion(
    IQualiaDbContext db,
    IUsuarioActual usuarioActual,
    IConsumoEvaluacion consumo,
    ISenalProcesamiento senal,
    ILogger<IniciarEvaluacion> logger)
{
    public async Task<EvaluacionAceptadaDto> EjecutarAsync(Guid llamadaId, CancellationToken ct)
    {
        var usuario = usuarioActual.RequerirRol(RolEmpresa.Supervisor);

        var llamada = await db.Llamadas.AsNoTracking()
            .Include(l => l.Rubrica)
            .FirstOrDefaultAsync(l => l.Id == llamadaId, ct)
            ?? throw ExcepcionNegocio.NoEncontrado("LLAMADA_NO_ENCONTRADA", "La llamada no existe.");

        var asignado = await db.UsuariosCampana.AnyAsync(
            uc => uc.UsuarioId == usuario.UsuarioId && uc.CampanaId == llamada.Rubrica!.CampanaId, ct);
        if (!asignado)
        {
            throw ExcepcionNegocio.Prohibido("CAMPANA_NO_ASIGNADA", "No está asignado a la campaña de esta llamada.");
        }

        if (llamada.Estado != EstadoLlamada.Disponible)
        {
            throw ExcepcionNegocio.Conflicto("EVALUACION_YA_INICIADA", "La evaluación de esta llamada ya fue iniciada.");
        }

        if (!llamada.Rubrica!.EstaPublicada)
        {
            throw ExcepcionNegocio.Conflicto("RUBRICA_SIN_PUBLICAR", "La rúbrica asignada a la llamada no está publicada.");
        }

        await using (var transaccion = await db.BeginTransactionAsync(ct))
        {
            // UPDATE ... WHERE estado = 'Disponible': bloquea la fila; si dos supervisores
            // lo intentan a la vez, el segundo encuentra 0 filas y recibe 409.
            var filas = await db.Llamadas
                .Where(l => l.Id == llamadaId && l.Estado == EstadoLlamada.Disponible)
                .ExecuteUpdateAsync(s => s.SetProperty(l => l.Estado, EstadoLlamada.EnCola), ct);

            if (filas == 0)
            {
                throw ExcepcionNegocio.Conflicto("EVALUACION_YA_INICIADA", "La evaluación de esta llamada ya fue iniciada.");
            }

            // Valida suscripción/cuota y reserva minutos dentro de esta misma transacción.
            // SUSCRIPCION_NO_ACTIVA (403) o CUOTA_AGOTADA (409) revierten todo.
            (await consumo.ReservarAsync(new ReservarConsumoRequest(usuario.EmpresaId, llamadaId, llamada.DuracionSeg), ct))
                .ValorOLanzar();

            db.TrabajosProcesamiento.Add(TrabajoProcesamiento.Crear(usuario.EmpresaId, llamadaId));

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                // Índice único (empresa_id, llamada_id): ya existe un trabajo para la llamada.
                logger.LogWarning(ex, "No se pudo crear el trabajo de la llamada {LlamadaId}", llamadaId);
                throw ExcepcionNegocio.Conflicto("EVALUACION_YA_INICIADA", "La evaluación de esta llamada ya fue iniciada.");
            }

            await transaccion.CommitAsync(ct);
        }

        senal.Notificar();
        logger.LogInformation("Llamada {LlamadaId} encolada por el supervisor {UsuarioId}", llamadaId, usuario.UsuarioId);

        return new EvaluacionAceptadaDto(llamadaId, usuario.EmpresaId, EstadoLlamada.EnCola, $"/api/llamadas/{llamadaId}");
    }
}
