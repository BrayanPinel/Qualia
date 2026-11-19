using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Common.Errores;
using Qualia.Application.Common.Seguridad;
using Qualia.Application.Features.Llamadas.Dtos;
using Qualia.Application.Features.Procesamiento;
using Qualia.Domain.Enums;

namespace Qualia.Application.Features.Llamadas;

/// POST /api/empresas/{empresaId}/llamadas/{id}/evaluacion — inicio manual por el Supervisor asignado.
/// Normalmente no hace falta: el despachador automático encola las llamadas Disponible.
/// Sirve para adelantar una llamada concreta y para volver a evaluar una llamada perdida (Fallida).
public sealed class IniciarEvaluacion(
    IQualiaDbContext db,
    IUsuarioActual usuarioActual,
    EncoladorEvaluacion encolador,
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

        if (llamada.Estado is not (EstadoLlamada.Disponible or EstadoLlamada.Fallida))
        {
            throw EncoladorEvaluacion.EvaluacionYaIniciada();
        }

        if (!llamada.Rubrica!.EstaPublicada)
        {
            throw ExcepcionNegocio.Conflicto("RUBRICA_SIN_PUBLICAR", "La rúbrica asignada a la llamada no está publicada.");
        }

        var reintento = llamada.Estado == EstadoLlamada.Fallida;
        await encolador.EncolarAsync(usuario.EmpresaId, llamadaId, llamada.DuracionSeg, ct, reintentarPerdida: reintento);

        senal.Notificar();
        logger.LogInformation(
            reintento ? "Llamada perdida {LlamadaId} reencolada por el supervisor {UsuarioId}" : "Llamada {LlamadaId} encolada por el supervisor {UsuarioId}",
            llamadaId, usuario.UsuarioId);

        return new EvaluacionAceptadaDto(llamadaId, usuario.EmpresaId, EstadoLlamada.EnCola, $"/api/empresas/{usuario.EmpresaId}/llamadas/{llamadaId}");
    }
}
