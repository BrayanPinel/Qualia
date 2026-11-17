using Qualia.Application.Common.Resultados;

namespace Qualia.Application.Features.Suscripciones.Comun;

/// <summary>Códigos de error de negocio del módulo de suscripciones (fase1 §7.8 y §7.10).</summary>
public static class ErroresSuscripcion
{
    public const string CodigoSuscripcionNoEncontrada = "SUSCRIPCION_NO_ENCONTRADA";
    public const string CodigoSuscripcionYaExiste = "SUSCRIPCION_YA_EXISTE";
    public const string CodigoSuscripcionNoActiva = "SUSCRIPCION_NO_ACTIVA";
    public const string CodigoPagoRechazado = "PAGO_RECHAZADO";
    public const string CodigoPlanNoDisponible = "PLAN_NO_DISPONIBLE";
    public const string CodigoFuncionNoIncluida = "FUNCION_NO_INCLUIDA";
    public const string CodigoCuotaAgotada = "CUOTA_AGOTADA";
    public const string CodigoCambioPlanPeriodoEnUso = "CAMBIO_PLAN_PERIODO_EN_USO";
    public const string CodigoEmpresaNoIdentificada = "EMPRESA_NO_IDENTIFICADA";
    public const string CodigoEmpresaNoEncontrada = "EMPRESA_NO_ENCONTRADA";
    public const string CodigoConsumoNoEncontrado = "CONSUMO_NO_ENCONTRADO";

    public static ErrorAplicacion SuscripcionNoEncontrada { get; } = ErrorAplicacion.NoEncontrado(
        CodigoSuscripcionNoEncontrada,
        "La empresa no tiene una suscripción registrada.");

    public static ErrorAplicacion SuscripcionYaExiste { get; } = ErrorAplicacion.Conflicto(
        CodigoSuscripcionYaExiste,
        "La empresa ya tiene una suscripción vigente.");

    public static ErrorAplicacion SuscripcionNoActiva { get; } = ErrorAplicacion.Prohibido(
        CodigoSuscripcionNoActiva,
        "La suscripción de la empresa no permite iniciar nuevas evaluaciones.");

    public static ErrorAplicacion PagoRechazado { get; } = ErrorAplicacion.PagoRequerido(
        CodigoPagoRechazado,
        "El pago simulado fue rechazado. No se acreditaron minutos ni se renovó la suscripción.");

    public static ErrorAplicacion EmpresaNoIdentificada { get; } = ErrorAplicacion.Prohibido(
        CodigoEmpresaNoIdentificada,
        "La sesión no identifica una empresa.");

    public static ErrorAplicacion EmpresaNoEncontrada { get; } = ErrorAplicacion.NoEncontrado(
        CodigoEmpresaNoEncontrada,
        "No se encontró la empresa solicitada.");

    public static ErrorAplicacion CuotaAgotada { get; } = ErrorAplicacion.Conflicto(
        CodigoCuotaAgotada,
        "La empresa no tiene minutos disponibles para esta llamada.");

    public static ErrorAplicacion CambioPlanPeriodoEnUso { get; } = ErrorAplicacion.Conflicto(
        CodigoCambioPlanPeriodoEnUso,
        "No se puede cambiar de plan porque el periodo del mes actual ya registra consumo de llamadas.");

    public static ErrorAplicacion ConsumoNoEncontrado { get; } = ErrorAplicacion.NoEncontrado(
        CodigoConsumoNoEncontrado,
        "No se encontró un registro de consumo para la llamada indicada.");

    public static ErrorAplicacion FuncionNoIncluida(string codigoFuncion) => ErrorAplicacion.Prohibido(
        CodigoFuncionNoIncluida,
        $"El plan contratado no incluye la función '{codigoFuncion}'.");

    public static ErrorAplicacion PlanNoDisponible { get; } = ErrorAplicacion.Validacion(
        new Dictionary<string, string[]>
        {
            ["planId"] = ["El plan seleccionado no existe o no está activo."],
        });
}
