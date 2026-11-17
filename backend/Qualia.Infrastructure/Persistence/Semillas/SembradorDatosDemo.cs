using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qualia.Domain.Entidades;
using Qualia.Domain.Enums;
using Qualia.Infrastructure.IA;

namespace Qualia.Infrastructure.Persistence.Semillas;

/// <summary>
/// Precarga de llamadas ficticias
/// con los datos mínimos que necesitan: empresas, usuarios, campañas y rúbricas publicadas.
/// Solo para desarrollo/demostración. Es idempotente (no duplica si ya existe la empresa Norte).
///
/// NOTA: los usuarios se crean sin contraseña utilizable ("demo-sin-credenciales");
/// </summary>
public sealed class SembradorDatosDemo(
    QualiaDbContext db,
    TimeProvider reloj,
    LectorGroundData groundData,
    ILogger<SembradorDatosDemo> logger)
{
    private const string HashNoUtilizable = "demo-sin-credenciales";

    public async Task SembrarAsync(CancellationToken ct = default)
    {
        if (await db.Empresas.AnyAsync(e => e.Id == DatosDemo.Norte.Empresa, ct))
        {
            await SincronizarDuracionesAsync(ct);
            return;
        }

        var ahora = reloj.GetUtcNow().UtcDateTime;
        SembrarNorte(ahora);
        SembrarSur(ahora);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Datos de demostración del módulo de llamadas cargados (empresas demo-norte y demo-sur)");
    }

    /// <summary>
    /// La duración registrada debe coincidir con la del audio.
    /// Si los audios se regeneraron, actualiza las llamadas demo que aún no se han procesado.
    /// </summary>
    private async Task SincronizarDuracionesAsync(CancellationToken ct)
    {
        var demo = DatosDemo.Norte.Llamadas.Concat(DatosDemo.Sur.Llamadas).Append(DatosDemo.Norte.LlamadaRubricaBorrador).ToList();
        var llamadas = await db.Llamadas.IgnoreQueryFilters()
            .Where(l => demo.Contains(l.Id) && l.Estado == EstadoLlamada.Disponible)
            .ToListAsync(ct);

        var actualizadas = 0;
        foreach (var llamada in llamadas)
        {
            var duracion = Duracion(llamada.ArchivoRuta, llamada.DuracionSeg);
            if (duracion != llamada.DuracionSeg)
            {
                llamada.DuracionSeg = duracion;
                actualizadas++;
            }
        }

        if (actualizadas > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Duración de {Cantidad} llamada(s) demo sincronizada con su audio", actualizadas);
        }
    }

    /// <summary>Duración real del audio según su ground data; si no existe, la de respaldo.</summary>
    private decimal Duracion(string archivoRuta, decimal respaldo) => groundData.Leer(archivoRuta)?.DuracionSeg ?? respaldo;

    private void SembrarNorte(DateTime ahora)
    {
        var e = DatosDemo.Norte.Empresa;
        db.Empresas.Add(new Empresa { Id = e, Codigo = "demo-norte", Nombre = "Contact Center Norte (demo)", FechaRegistro = ahora });

        db.Usuarios.AddRange(
            Usuario(e, DatosDemo.Norte.Admin, "Ana Administradora", "ana.admin@norte.demo", RolEmpresa.AdminEmpresa),
            Usuario(e, DatosDemo.Norte.Supervisora, "Sofía Supervisora", "sofia.supervisora@norte.demo", RolEmpresa.Supervisor),
            Usuario(e, DatosDemo.Norte.SupervisorSinCampana, "Samuel Supervisor", "samuel.supervisor@norte.demo", RolEmpresa.Supervisor),
            Usuario(e, DatosDemo.Norte.AgenteAndres, "Andrés Agente", "andres.agente@norte.demo", RolEmpresa.Agente),
            Usuario(e, DatosDemo.Norte.AgenteAlicia, "Alicia Agente", "alicia.agente@norte.demo", RolEmpresa.Agente),
            Usuario(e, DatosDemo.Norte.Auditor, "Arturo Auditor", "arturo.auditor@norte.demo", RolEmpresa.Auditor));

        db.Campanas.Add(new Campana { EmpresaId = e, Id = DatosDemo.Norte.Campana, Nombre = "Atención Hogar" });
        db.UsuariosCampana.AddRange(
            Asignacion(e, DatosDemo.Norte.Supervisora, DatosDemo.Norte.Campana),
            Asignacion(e, DatosDemo.Norte.AgenteAndres, DatosDemo.Norte.Campana),
            Asignacion(e, DatosDemo.Norte.AgenteAlicia, DatosDemo.Norte.Campana));

        db.Rubricas.Add(new Rubrica
        {
            EmpresaId = e,
            Id = DatosDemo.Norte.Rubrica,
            CampanaId = DatosDemo.Norte.Campana,
            Nombre = "Calidad de atención",
            Version = 1,
            Estado = EstadoRubrica.Activa,
            FechaCreacion = ahora,
            Criterios =
            [
                Criterio(e, "Saludo y presentación", "El agente saluda, agradece la llamada y se presenta.", 20, false, 70),
                Criterio(e, "Verificación de identidad", "El agente verifica la identidad del cliente antes de revisar la cuenta.", 30, true, 80),
                Criterio(e, "Resolución de la consulta", "El agente explica una solución concreta para la consulta del cliente.", 30, false, 70),
                Criterio(e, "Cierre y despedida", "El agente ofrece ayuda adicional, agradece y se despide cordialmente.", 20, false, 60),
            ],
        });

        // Versión 2 en borrador: no puede usarse para evaluar.
        db.Rubricas.Add(new Rubrica
        {
            EmpresaId = e,
            Id = DatosDemo.Norte.RubricaBorrador,
            CampanaId = DatosDemo.Norte.Campana,
            Nombre = "Calidad de atención",
            Version = 2,
            Estado = EstadoRubrica.Borrador,
            FechaCreacion = ahora,
            Criterios = [Criterio(e, "Saludo y presentación", "El agente saluda y se presenta.", 100, false, 70)],
        });

        decimal[] duraciones = [182.5m, 245m, 310.75m, 128m, 400m, 95m];
        for (var i = 0; i < DatosDemo.Norte.Llamadas.Length; i++)
        {
            var agente = i % 2 == 0 ? DatosDemo.Norte.AgenteAndres : DatosDemo.Norte.AgenteAlicia;
            db.Llamadas.Add(Llamada(e, DatosDemo.Norte.Llamadas[i], DatosDemo.Norte.Rubrica, agente,
                $"demo/norte/llamada-{i + 1:00}.wav", Duracion($"demo/norte/llamada-{i + 1:00}.wav", duraciones[i]), ahora.AddDays(-(i + 1))));
        }

        db.Llamadas.Add(Llamada(e, DatosDemo.Norte.LlamadaRubricaBorrador, DatosDemo.Norte.RubricaBorrador,
            DatosDemo.Norte.AgenteAndres, "demo/norte/llamada-99.wav", Duracion("demo/norte/llamada-99.wav", 60m), ahora.AddDays(-10)));
    }

    private void SembrarSur(DateTime ahora)
    {
        var e = DatosDemo.Sur.Empresa;
        db.Empresas.Add(new Empresa { Id = e, Codigo = "demo-sur", Nombre = "Cobranzas Sur (demo)", FechaRegistro = ahora });

        db.Usuarios.AddRange(
            Usuario(e, DatosDemo.Sur.Supervisor, "Silvia Supervisora", "silvia.supervisora@sur.demo", RolEmpresa.Supervisor),
            Usuario(e, DatosDemo.Sur.Agente, "Adrián Agente", "adrian.agente@sur.demo", RolEmpresa.Agente));

        db.Campanas.Add(new Campana { EmpresaId = e, Id = DatosDemo.Sur.Campana, Nombre = "Cobranza preventiva" });
        db.UsuariosCampana.AddRange(
            Asignacion(e, DatosDemo.Sur.Supervisor, DatosDemo.Sur.Campana),
            Asignacion(e, DatosDemo.Sur.Agente, DatosDemo.Sur.Campana));

        db.Rubricas.Add(new Rubrica
        {
            EmpresaId = e,
            Id = DatosDemo.Sur.Rubrica,
            CampanaId = DatosDemo.Sur.Campana,
            Nombre = "Cobranza respetuosa",
            Version = 1,
            Estado = EstadoRubrica.Activa,
            FechaCreacion = ahora,
            Criterios =
            [
                Criterio(e, "Saludo y presentación", "El agente saluda y se presenta con su nombre.", 25, false, 70),
                Criterio(e, "Verificación de identidad", "El agente verifica la identidad del titular antes de hablar de la deuda.", 40, true, 80),
                Criterio(e, "Acuerdo de pago", "El agente propone una solución o acuerdo de pago claro.", 35, false, 70),
            ],
        });

        decimal[] duraciones = [210m, 156.25m, 333m];
        for (var i = 0; i < DatosDemo.Sur.Llamadas.Length; i++)
        {
            db.Llamadas.Add(Llamada(e, DatosDemo.Sur.Llamadas[i], DatosDemo.Sur.Rubrica, DatosDemo.Sur.Agente,
                $"demo/sur/llamada-{i + 1:00}.wav", Duracion($"demo/sur/llamada-{i + 1:00}.wav", duraciones[i]), ahora.AddDays(-(i + 1))));
        }
    }

    private static Usuario Usuario(Guid empresa, Guid id, string nombre, string correo, RolEmpresa rol) => new()
    {
        EmpresaId = empresa, Id = id, Nombre = nombre, Correo = correo, Rol = rol, Activo = true, PasswordHash = HashNoUtilizable,
    };

    private static UsuarioCampana Asignacion(Guid empresa, Guid usuario, Guid campana) => new()
    {
        EmpresaId = empresa, UsuarioId = usuario, CampanaId = campana,
    };

    private static CriterioRubrica Criterio(Guid empresa, string nombre, string descripcion, decimal peso, bool critico, decimal umbral) => new()
    {
        EmpresaId = empresa, Nombre = nombre, Descripcion = descripcion, Peso = peso, EsCritico = critico, UmbralCumplimiento = umbral,
    };

    private static Llamada Llamada(Guid empresa, Guid id, Guid rubrica, Guid agente, string ruta, decimal duracion, DateTime fecha) => new()
    {
        EmpresaId = empresa,
        Id = id,
        RubricaId = rubrica,
        AgenteId = agente,
        ArchivoRuta = ruta,
        DuracionSeg = duracion,
        FechaLlamada = fecha,
        FechaRegistro = fecha,
        Estado = EstadoLlamada.Disponible,
    };
}
