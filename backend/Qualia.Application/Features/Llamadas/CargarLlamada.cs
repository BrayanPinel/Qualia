using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qualia.Application.Common.Errores;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Common.Seguridad;
using Qualia.Application.Features.Llamadas.Dtos;
using Qualia.Application.Features.Procesamiento;
using Qualia.Domain.Entidades;
using Qualia.Domain.Enums;

namespace Qualia.Application.Features.Llamadas;

/// POST /api/empresas/{empresaId}/llamadas — la empresa sube una grabación propia
/// (por ejemplo un audio de WhatsApp) para una campaña y un agente.
/// Solo responde si Procesamiento:CargaManualHabilitada es true (herramienta de pruebas del backend).
/// La llamada nace Disponible con la rúbrica publicada vigente fijada (RN-07); desde ahí
/// el despachador automático la encola igual que una llamada precargada.
public sealed class CargarLlamada(
    IQualiaDbContext db,
    IUsuarioActual usuarioActual,
    IValidator<CargarLlamadaSolicitud> validador,
    IMedidorDuracionAudio medidor,
    IGuardadoAudio guardado,
    IRegistroAuditoria auditoria,
    ISenalProcesamiento senal,
    TimeProvider reloj,
    IOptions<OpcionesProcesamiento> opciones,
    ILogger<CargarLlamada> logger)
{
    public const long TamanoMaximoBytes = 25 * 1024 * 1024;

    /// Límite práctico del proveedor de evaluación (Groq gratuito) y del reproductor de demostración.
    public const decimal DuracionMaximaSeg = 1800m;

    public static readonly string[] ExtensionesPermitidas = [".ogg", ".opus", ".mp3", ".wav", ".m4a"];

    public async Task<LlamadaDto> EjecutarAsync(CargarLlamadaSolicitud solicitud, CancellationToken ct)
    {
        if (!opciones.Value.CargaManualHabilitada)
        {
            throw ExcepcionNegocio.Prohibido("CARGA_MANUAL_DESHABILITADA", "La carga manual de llamadas no está habilitada.");
        }

        var usuario = usuarioActual.RequerirRol(RolEmpresa.AdminEmpresa, RolEmpresa.Supervisor);
        await validador.ValidarOLanzarAsync(solicitud, ct);

        var campana = await db.Campanas.AsNoTracking().FirstOrDefaultAsync(c => c.Id == solicitud.CampanaId, ct)
            ?? throw ExcepcionNegocio.NoEncontrado("CAMPANA_NO_ENCONTRADA", "La campaña no existe.");

        if (usuario.Rol == RolEmpresa.Supervisor && !await AsignadoAsync(usuario.UsuarioId, campana.Id, ct))
        {
            throw ExcepcionNegocio.Prohibido("CAMPANA_NO_ASIGNADA", "No está asignado a esta campaña.");
        }

        if (!campana.Activo)
        {
            throw ExcepcionNegocio.Conflicto("CAMPANA_INACTIVA", "La campaña está inactiva.");
        }

        var rubrica = await db.Rubricas.AsNoTracking()
            .Where(r => r.CampanaId == campana.Id && r.Estado == EstadoRubrica.Activa)
            .Select(r => new { r.Id, r.Version })
            .FirstOrDefaultAsync(ct)
            ?? throw ExcepcionNegocio.Conflicto("RUBRICA_SIN_PUBLICAR", "La campaña no tiene una rúbrica publicada.");

        var agenteValido = await db.Usuarios.AnyAsync(
            u => u.Id == solicitud.AgenteId && u.Rol == RolEmpresa.Agente && u.Activo, ct);
        if (!agenteValido || !await AsignadoAsync(solicitud.AgenteId, campana.Id, ct))
        {
            throw ErrorCampo("agenteId", "El agente debe existir, estar activo y asignado a la campaña.");
        }

        var audio = await LeerAudioAsync(solicitud.Contenido, ct);
        var extension = Path.GetExtension(solicitud.NombreArchivo).ToLowerInvariant();
        var duracion = medidor.Medir(audio, extension);
        if (duracion is not > 0m)
        {
            throw ErrorCampo("audio", "No se pudo leer la duración del audio; verifique que el archivo no esté dañado.");
        }

        if (duracion > DuracionMaximaSeg)
        {
            throw ErrorCampo("audio", $"La grabación no puede durar más de {DuracionMaximaSeg / 60m:0} minutos.");
        }

        var ahora = reloj.GetUtcNow().UtcDateTime;
        var llamada = new Llamada
        {
            EmpresaId = usuario.EmpresaId,
            RubricaId = rubrica.Id,
            AgenteId = solicitud.AgenteId,
            DuracionSeg = Math.Round(duracion.Value, 2, MidpointRounding.AwayFromZero),
            FechaLlamada = solicitud.FechaLlamada?.ToUniversalTime() ?? ahora,
            FechaRegistro = ahora,
            Estado = EstadoLlamada.Disponible,
        };
        llamada.ArchivoRuta = $"cargas/{usuario.EmpresaId}/{llamada.Id}{extension}";

        using (var flujo = new MemoryStream(audio, writable: false))
        {
            await guardado.GuardarAsync(llamada.ArchivoRuta, flujo, ct);
        }

        db.Llamadas.Add(llamada);
        await auditoria.RegistrarAsync(usuario.EmpresaId, usuario.UsuarioId, "CargarLlamada", "Llamada", llamada.Id,
            despues: new { CampanaId = campana.Id, llamada.AgenteId, llamada.RubricaId, llamada.DuracionSeg, Formato = extension },
            cancellationToken: ct);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            // Sin fila no hay quien referencie el archivo: se borra para no dejar huérfanos.
            await guardado.EliminarAsync(llamada.ArchivoRuta, CancellationToken.None);
            throw;
        }

        senal.Notificar();
        logger.LogInformation("Llamada {LlamadaId} cargada por {UsuarioId} ({Duracion} s, {Formato})",
            llamada.Id, usuario.UsuarioId, llamada.DuracionSeg, extension);

        return new LlamadaDto(llamada.Id, llamada.EmpresaId, campana.Id, llamada.AgenteId, rubrica.Id, rubrica.Version,
            llamada.DuracionSeg, llamada.Estado, llamada.FechaLlamada, MotivoFallo: null, RequiereRevision: null);
    }

    private Task<bool> AsignadoAsync(Guid usuarioId, Guid campanaId, CancellationToken ct) =>
        db.UsuariosCampana.AnyAsync(uc => uc.UsuarioId == usuarioId && uc.CampanaId == campanaId, ct);

    /// Se lee con tope para no cargar en memoria más de lo permitido aunque el cliente mienta en el tamaño.
    private static async Task<byte[]> LeerAudioAsync(Stream contenido, CancellationToken ct)
    {
        using var memoria = new MemoryStream();
        var bufer = new byte[81920];
        int leidos;
        while ((leidos = await contenido.ReadAsync(bufer, ct)) > 0)
        {
            if (memoria.Length + leidos > TamanoMaximoBytes)
            {
                throw ErrorCampo("audio", $"El archivo no puede superar {TamanoMaximoBytes / (1024 * 1024)} MB.");
            }

            memoria.Write(bufer, 0, leidos);
        }

        if (memoria.Length == 0)
        {
            throw ErrorCampo("audio", "El archivo de audio está vacío.");
        }

        return memoria.ToArray();
    }

    private static ExcepcionNegocio ErrorCampo(string campo, string mensaje) =>
        ExcepcionNegocio.Validacion(new Dictionary<string, string[]> { [campo] = [mensaje] });
}

/// Datos de la carga ya separados de HTTP: el controlador entrega el flujo del archivo.
public sealed record CargarLlamadaSolicitud(
    Guid CampanaId,
    Guid AgenteId,
    DateTime? FechaLlamada,
    string NombreArchivo,
    Stream Contenido);
