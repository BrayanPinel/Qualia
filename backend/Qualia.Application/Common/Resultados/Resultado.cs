using System.Diagnostics.CodeAnalysis;

namespace Qualia.Application.Common.Resultados;

/// <summary>
/// Resultado de un caso de uso sin valor de retorno: éxito o <see cref="ErrorAplicacion"/>.
/// </summary>
public sealed class Resultado
{
    private static readonly Resultado InstanciaExito = new();

    private Resultado()
    {
    }

    private Resultado(ErrorAplicacion error)
    {
        Error = error ?? throw new ArgumentNullException(nameof(error));
    }

    [MemberNotNullWhen(false, nameof(Error))]
    public bool EsExitoso => Error is null;

    public ErrorAplicacion? Error { get; }

    public static Resultado Exito() => InstanciaExito;

    public static Resultado Fallo(ErrorAplicacion error) => new(error);

    public static implicit operator Resultado(ErrorAplicacion error) => Fallo(error);
}

/// <summary>
/// Resultado de un caso de uso: valor exitoso o <see cref="ErrorAplicacion"/>.
/// Evita usar excepciones para el flujo de negocio esperado (404, 409, pago rechazado…).
/// </summary>
public sealed class Resultado<T>
{
    private Resultado(T valor)
    {
        ArgumentNullException.ThrowIfNull(valor);
        Valor = valor;
    }

    private Resultado(ErrorAplicacion error)
    {
        Error = error ?? throw new ArgumentNullException(nameof(error));
    }

    [MemberNotNullWhen(true, nameof(Valor))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool EsExitoso => Error is null;

    public T? Valor { get; }

    public ErrorAplicacion? Error { get; }

    public static Resultado<T> Exito(T valor) => new(valor);

    public static Resultado<T> Fallo(ErrorAplicacion error) => new(error);

    public static implicit operator Resultado<T>(T valor) => Exito(valor);

    public static implicit operator Resultado<T>(ErrorAplicacion error) => Fallo(error);
}
