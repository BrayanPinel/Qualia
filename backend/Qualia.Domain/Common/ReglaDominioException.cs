namespace Qualia.Domain.Common;

/// <summary>
/// Se lanza cuando una operación viola una invariante del dominio
/// (por ejemplo, una transición de estado no permitida).
/// </summary>
public sealed class ReglaDominioException(string codigo, string mensaje) : Exception(mensaje)
{
    public string Codigo { get; } = codigo;
}
