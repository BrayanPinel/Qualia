namespace Qualia.Application.Features.Procesamiento;

/// <summary>
/// Uso técnico de los proveedores de IA en UN intento de procesamiento.
/// Se informa al módulo de consumo con IConsumoEvaluacion.RegistrarUsoTecnicoAsync,
/// que lo acumula en Consumo_llamada incluso si la llamada falla.
/// </summary>
public sealed record UsoTecnicoIA(
    int TokensTranscriptor,
    int TokensEntradaEvaluador,
    int TokensSalidaEvaluador,
    decimal CostoEstimado)
{
    public static readonly UsoTecnicoIA Cero = new(0, 0, 0, 0m);

    public bool EsCero => this == Cero;

    public UsoTecnicoIA Sumar(UsoTecnicoIA otro) => new(
        TokensTranscriptor + otro.TokensTranscriptor,
        TokensEntradaEvaluador + otro.TokensEntradaEvaluador,
        TokensSalidaEvaluador + otro.TokensSalidaEvaluador,
        CostoEstimado + otro.CostoEstimado);
}
