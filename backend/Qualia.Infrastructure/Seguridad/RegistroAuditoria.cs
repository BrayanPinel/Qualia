using System.Text.Json;
using System.Text.Json.Nodes;
using Qualia.Application.Common.Interfaces;
using Qualia.Domain.Entidades;
using Qualia.Infrastructure.Persistence;

namespace Qualia.Infrastructure.Seguridad;

/// <summary>
/// <see cref="IRegistroAuditoria"/> sobre la tabla auditoria. Solo agrega la fila al contexto del scope:
/// el llamador la guarda con su propio SaveChanges y dentro de su transacción, así la auditoría
/// se confirma o se revierte junto con la operación auditada.
/// </summary>
public sealed class RegistroAuditoria(QualiaDbContext db, TimeProvider reloj) : IRegistroAuditoria
{
    private const int LongitudMaxima = 100;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Fragmentos de nombre de propiedad que nunca se guardan en antes/despues.</summary>
    private static readonly string[] FragmentosSensibles =
        ["password", "contrasena", "contraseña", "hash", "token", "secret", "secreto", "apikey", "clave"];

    public Task RegistrarAsync(
        Guid empresaId,
        Guid? usuarioId,
        string accion,
        string entidad,
        Guid? entidadId,
        object? antes = null,
        object? despues = null,
        CancellationToken cancellationToken = default)
    {
        if (empresaId == Guid.Empty)
        {
            throw new ArgumentException("La auditoría requiere una empresa.", nameof(empresaId));
        }

        db.Auditorias.Add(new Auditoria
        {
            EmpresaId = empresaId,
            UsuarioId = usuarioId,
            Accion = Texto(accion, nameof(accion)),
            Entidad = Texto(entidad, nameof(entidad)),
            EntidadId = entidadId,
            Antes = Serializar(antes),
            Despues = Serializar(despues),
            Fecha = reloj.GetUtcNow().UtcDateTime,
        });

        return Task.CompletedTask;
    }

    /// <summary>Serializa a JSON quitando, a cualquier profundidad, las propiedades con nombre sensible.</summary>
    public static string? Serializar(object? valor)
    {
        if (valor is null)
        {
            return null;
        }

        var nodo = JsonSerializer.SerializeToNode(valor, valor.GetType(), Json);
        QuitarSensibles(nodo);
        return nodo?.ToJsonString(Json);
    }

    private static void QuitarSensibles(JsonNode? nodo)
    {
        switch (nodo)
        {
            case JsonObject objeto:
                foreach (var nombre in objeto.Select(p => p.Key).Where(EsSensible).ToList())
                {
                    objeto.Remove(nombre);
                }

                foreach (var propiedad in objeto)
                {
                    QuitarSensibles(propiedad.Value);
                }

                break;
            case JsonArray arreglo:
                foreach (var elemento in arreglo)
                {
                    QuitarSensibles(elemento);
                }

                break;
        }
    }

    private static bool EsSensible(string nombre) =>
        FragmentosSensibles.Any(f => nombre.Contains(f, StringComparison.OrdinalIgnoreCase));

    private static string Texto(string valor, string parametro)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(valor, parametro);
        var limpio = valor.Trim();
        if (limpio.Length > LongitudMaxima)
        {
            throw new ArgumentException($"Máximo {LongitudMaxima} caracteres.", parametro);
        }

        return limpio;
    }
}
