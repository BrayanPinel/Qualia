using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Common.Resultados;
using Qualia.Domain.Entidades;
using Qualia.Domain.Enums;

namespace Qualia.Application.Features.Procesamiento;

/// <summary>
/// Ejecuta UN intento de procesamiento de un trabajo ya reclamado (estado EnProceso).
/// Debe ejecutarse en un scope cuyo IContextoEmpresa se estableció con la empresa del trabajo,
/// de modo que todas las consultas quedan filtradas por esa empresa.
///
/// Fases idempotentes (un reintento retoma donde quedó, sin duplicar):
/// 1. Transcripción + enmascarado → Segmento_transcripcion (se omite si ya existen segmentos).
/// 2. Evaluación + validación de evidencia → Evaluacion_IA (se omite si ya existe).
/// 3. Cierre en una transacción: llamada EvaluadaPorIA, trabajo Completado y consumo Facturado.
/// </summary>
public sealed class ProcesadorTrabajo(
    IQualiaDbContext db,
    ITranscriptor transcriptor,
    IEvaluadorIA evaluador,
    IEnmascaradorDatos enmascarador,
    IConsumoEvaluacion consumo,
    TimeProvider reloj,
    IOptions<OpcionesProcesamiento> opciones,
    ILogger<ProcesadorTrabajo> logger)
{
    private readonly OpcionesProcesamiento _opciones = opciones.Value;

    public async Task ProcesarAsync(Guid trabajoId, CancellationToken cancellationToken)
    {
        // Uso técnico de este intento;.
        var uso = UsoTecnicoIA.Cero;
        try
        {
            await EjecutarFasesAsync(trabajoId, u => uso = u, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Apagado ordenado: el bloqueo expirará y el trabajo se retomará.
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falló el intento de procesamiento del trabajo {TrabajoId}", trabajoId);
            await RegistrarFalloAsync(trabajoId, ex, uso, cancellationToken);
        }
    }

    private async Task EjecutarFasesAsync(Guid trabajoId, Action<UsoTecnicoIA> reportarUso, CancellationToken ct)
    {
        var uso = UsoTecnicoIA.Cero;
        var trabajo = await db.TrabajosProcesamiento.FirstOrDefaultAsync(t => t.Id == trabajoId, ct)
                      ?? throw new ErrorProcesamientoPermanente("El trabajo de procesamiento no existe.");

        if (trabajo.Estado != EstadoTrabajo.EnProceso)
        {
            return; // Otro proceso ya lo terminó.
        }

        if (trabajo.Intentos > _opciones.MaximoIntentos)
        {
            throw new ErrorProcesamientoPermanente("Se agotaron los reintentos de procesamiento.");
        }

        var llamada = await db.Llamadas.FirstOrDefaultAsync(l => l.Id == trabajo.LlamadaId, ct)
                      ?? throw new ErrorProcesamientoPermanente("La llamada asociada no existe.");

        llamada.ReanudarEnCola();

        // ---------- Fase 1: transcripción ----------
        var segmentos = await db.SegmentosTranscripcion
            .Where(s => s.LlamadaId == llamada.Id)
            .OrderBy(s => s.Orden)
            .ToListAsync(ct);

        if (segmentos.Count == 0)
        {
            llamada.CambiarEstado(EstadoLlamada.Transcribiendo);
            await db.SaveChangesAsync(ct);

            var transcripcion = await transcriptor.TranscribirAsync(
                new SolicitudTranscripcion(llamada.EmpresaId, llamada.Id, llamada.ArchivoRuta, llamada.DuracionSeg), ct);

            uso = uso.Sumar(new UsoTecnicoIA(transcripcion.TokensConsumidos, 0, 0, transcripcion.CostoEstimado));
            reportarUso(uso);

            segmentos = ConstruirSegmentos(llamada, transcripcion.Segmentos);
            db.SegmentosTranscripcion.AddRange(segmentos);
        }

        llamada.CambiarEstado(EstadoLlamada.Transcrita);
        await db.SaveChangesAsync(ct);

        // ---------- Fase 2: evaluación ----------
        var evaluacionExiste = await db.EvaluacionesIA.AnyAsync(e => e.LlamadaId == llamada.Id, ct);
        EvaluacionIA? nuevaEvaluacion = null;

        if (!evaluacionExiste)
        {
            llamada.CambiarEstado(EstadoLlamada.Evaluando);
            await db.SaveChangesAsync(ct);

            var criterios = await db.CriteriosRubrica
                .Where(c => c.RubricaId == llamada.RubricaId)
                .OrderBy(c => c.Nombre)
                .ToListAsync(ct);

            if (criterios.Count == 0)
            {
                throw new ErrorProcesamientoPermanente("La rúbrica de la llamada no tiene criterios.");
            }

            var solicitud = new SolicitudEvaluacion(
                llamada.EmpresaId,
                llamada.Id,
                criterios.Select(c => new CriterioParaEvaluar(c.Id, c.Nombre, c.Descripcion, c.Peso, c.EsCritico, c.UmbralCumplimiento)).ToList(),
                segmentos.Select(s => new SegmentoParaEvaluar(s.Id, s.Orden, s.Hablante, s.Texto)).ToList());

            var respuesta = await evaluador.EvaluarAsync(solicitud, ct);
            uso = uso.Sumar(new UsoTecnicoIA(0, respuesta.TokensEntrada, respuesta.TokensSalida, respuesta.CostoEstimado));
            reportarUso(uso);

            var construida = ConstructorEvaluacion.Construir(
                llamada.EmpresaId, llamada.Id, respuesta, criterios, segmentos, reloj.GetUtcNow().UtcDateTime);

            if (construida.EvidenciasDescartadas > 0)
            {
                logger.LogInformation(
                    "Llamada {LlamadaId}: se descartaron {Cantidad} evidencias inválidas; requiere revisión humana",
                    llamada.Id, construida.EvidenciasDescartadas);
            }

            nuevaEvaluacion = construida.Evaluacion;
        }

        // ---------- Fase 3: cierre atómico ----------
        await using var transaccion = await db.BeginTransactionAsync(ct);

        if (nuevaEvaluacion is not null)
        {
            db.EvaluacionesIA.Add(nuevaEvaluacion);
        }

        llamada.CambiarEstado(EstadoLlamada.Evaluando);
        llamada.CambiarEstado(EstadoLlamada.EvaluadaPorIA);
        trabajo.Completar();
        await db.SaveChangesAsync(ct);

        await RegistrarUsoTecnicoAsync(llamada, uso, ct);
        (await consumo.FacturarAsync(llamada.EmpresaId, llamada.Id, ct)).LanzarSiFallo();
        await db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);

        logger.LogInformation("Llamada {LlamadaId} evaluada por IA (trabajo {TrabajoId})", llamada.Id, trabajo.Id);
    }

    /// <summary>Acumula en el consumo de la llamada el costo técnico de este intento (incluso si falla).</summary>
    private async Task RegistrarUsoTecnicoAsync(Llamada llamada, UsoTecnicoIA uso, CancellationToken ct)
    {
        if (uso.EsCero)
        {
            return;
        }

        (await consumo.RegistrarUsoTecnicoAsync(new RegistrarUsoTecnicoRequest(
            llamada.EmpresaId, llamada.Id,
            uso.TokensTranscriptor, uso.TokensEntradaEvaluador, uso.TokensSalidaEvaluador, uso.CostoEstimado), ct))
            .LanzarSiFallo();
    }

    private List<SegmentoTranscripcion> ConstruirSegmentos(Llamada llamada, IReadOnlyList<SegmentoTranscrito> crudos)
    {
        if (crudos.Count == 0)
        {
            throw new ErrorProcesamiento("La transcripción no devolvió segmentos.");
        }

        var resultado = new List<SegmentoTranscripcion>(crudos.Count);
        var orden = 1;

        foreach (var crudo in crudos.OrderBy(s => s.InicioSeg))
        {
            var inicio = Math.Round(crudo.InicioSeg, 2);
            var fin = Math.Round(crudo.FinSeg, 2);

            if (inicio < 0 || fin < inicio || fin > llamada.DuracionSeg)
            {
                throw new ErrorProcesamiento("La transcripción contiene intervalos fuera de la duración de la llamada.");
            }

            // RN-14: el texto se enmascara ANTES de almacenarse o enviarse al evaluador.
            var texto = enmascarador.Enmascarar(crudo.Texto ?? string.Empty).Trim();
            if (texto.Length == 0)
            {
                continue;
            }

            var hablante = string.IsNullOrWhiteSpace(crudo.Hablante) ? "Desconocido" : crudo.Hablante.Trim();

            resultado.Add(new SegmentoTranscripcion
            {
                EmpresaId = llamada.EmpresaId,
                LlamadaId = llamada.Id,
                Orden = orden++,
                Hablante = hablante.Length <= 50 ? hablante : hablante[..50],
                InicioSeg = inicio,
                FinSeg = fin,
                Texto = texto,
            });
        }

        if (resultado.Count == 0)
        {
            throw new ErrorProcesamiento("La transcripción no contiene texto.");
        }

        return resultado;
    }

    /// <summary>
    /// Registra el fallo con datos frescos (se descarta lo pendiente del intento fallido).
    /// Fallo recuperable → ReintentoPendiente y la llamada vuelve a EnCola.
    /// Fallo definitivo → trabajo Fallido, llamada Fallida y consumo Liberado, en una transacción.
    /// </summary>
    private async Task RegistrarFalloAsync(Guid trabajoId, Exception error, UsoTecnicoIA uso, CancellationToken ct)
    {
        db.LimpiarSeguimiento();

        var trabajo = await db.TrabajosProcesamiento.FirstOrDefaultAsync(t => t.Id == trabajoId, ct);
        if (trabajo is null || trabajo.Estado != EstadoTrabajo.EnProceso)
        {
            return;
        }

        var llamada = await db.Llamadas.FirstAsync(l => l.Id == trabajo.LlamadaId, ct);
        var mensaje = SaneadorErrores.Sanear(error);

        var definitivo = SaneadorErrores.EsPermanente(error)
            ? FallarDefinitivamente(trabajo, mensaje)
            : trabajo.RegistrarFallo(mensaje, _opciones.MaximoIntentos, reloj.GetUtcNow().UtcDateTime, _opciones.EsperaBaseReintento);

        await using var transaccion = await db.BeginTransactionAsync(ct);

        if (definitivo)
        {
            llamada.ReanudarEnCola();
            llamada.CambiarEstado(EstadoLlamada.Fallida);
            await db.SaveChangesAsync(ct);
            await RegistrarUsoTecnicoAsync(llamada, uso, ct);
            (await consumo.LiberarAsync(llamada.EmpresaId, llamada.Id, ct)).LanzarSiFallo();
        }
        else
        {
            llamada.ReanudarEnCola();
        }

        await db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);

        if (definitivo)
        {
            logger.LogWarning("Trabajo {TrabajoId} falló definitivamente: {Motivo}", trabajoId, mensaje);
        }
    }

    private static bool FallarDefinitivamente(TrabajoProcesamiento trabajo, string mensaje)
    {
        trabajo.FallarDefinitivamente(mensaje);
        return true;
    }
}
