using Qualia.Domain.Common;

namespace Qualia.Domain.Suscripciones;

/// Auditoría de cambios comerciales hechos por PlatformAdmin (tabla global
/// auditoria_plataforma, HU-07). No concede acceso a contenido de llamadas.
/// antes/despues son JSON (jsonb) y nunca contienen contraseñas,
/// hashes, tokens ni secretos.
public sealed class AuditoriaPlataforma
{
    public const int LongitudMaximaAccion = 100;

    // Constructor para EF Core.
    private AuditoriaPlataforma()
    {
    }

    public Guid Id { get; private set; }

    public Guid AdministradorId { get; private set; }

    public Guid EmpresaObjetivoId { get; private set; }

    public string Accion { get; private set; } = string.Empty;

    /// Estado previo serializado como JSON (jsonb NULL).
    public string? Antes { get; private set; }

    /// Estado posterior serializado como JSON (jsonb NULL).
    public string? Despues { get; private set; }

    public DateTime Fecha { get; private set; }

    public static AuditoriaPlataforma Registrar(
        Guid administradorId,
        Guid empresaObjetivoId,
        string accion,
        string? antesJson,
        string? despuesJson,
        DateTime fechaUtc)
    {
        if (administradorId == Guid.Empty)
        {
            throw new ArgumentException("El administrador es obligatorio.", nameof(administradorId));
        }

        if (empresaObjetivoId == Guid.Empty)
        {
            throw new ArgumentException("La empresa objetivo es obligatoria.", nameof(empresaObjetivoId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(accion);
        var accionNormalizada = accion.Trim();
        if (accionNormalizada.Length > LongitudMaximaAccion)
        {
            throw new ArgumentException(
                $"La acción admite como máximo {LongitudMaximaAccion} caracteres.",
                nameof(accion));
        }

        var fechaValidada = FechasUtc.Exigir(fechaUtc, nameof(fechaUtc));

        return new AuditoriaPlataforma
        {
            Id = Guid.CreateVersion7(),
            AdministradorId = administradorId,
            EmpresaObjetivoId = empresaObjetivoId,
            Accion = accionNormalizada,
            Antes = antesJson,
            Despues = despuesJson,
            Fecha = fechaValidada,
        };
    }
}
