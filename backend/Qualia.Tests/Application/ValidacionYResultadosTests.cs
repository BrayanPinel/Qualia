using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Qualia.Application;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Common.Models;
using Qualia.Application.Common.Resultados;
using Qualia.Application.Features.Suscripciones.Consumo.ConsultarConsumo;
using Qualia.Application.Features.Suscripciones.CrearSuscripcionInicial;
using Qualia.Application.Features.Suscripciones.PagoSimulado;
using Qualia.Application.Features.Suscripciones.Plataforma.CambiarSuscripcion;

namespace Qualia.Tests.Application;

public sealed class ValidacionYResultadosTests
{
    [Fact]
    public void Resultado_NoPermiteExitoNuloYConvierteErrorADto()
    {
        Assert.Throws<ArgumentNullException>(() => Resultado<string>.Exito(null!));

        var sinValor = Resultado.Exito();
        Assert.True(sinValor.EsExitoso);

        var error = ErrorAplicacion.PagoRequerido("PAGO_RECHAZADO", "Pago rechazado.");
        Resultado resultadoFallo = error;
        Assert.False(resultadoFallo.EsExitoso);

        var dto = error.AErrorDto("trace-01");
        Assert.Equal(402, dto.Status);
        Assert.Equal("PAGO_RECHAZADO", dto.Codigo);
        Assert.Equal("trace-01", dto.TraceId);
    }

    [Fact]
    public async Task AddApplication_RegistraTodosLosValidadoresDeFluentValidation()
    {
        var services = new ServiceCollection();
        services.AddApplication();

        using var provider = services.BuildServiceProvider();

        var pagoValidator = provider.GetRequiredService<IValidator<PagoSimuladoDto>>();
        var errorPago = await pagoValidator.ValidarAsync(new PagoSimuladoDto("Invalido"));
        Assert.NotNull(errorPago);
        Assert.Equal(TipoError.Validacion, errorPago.Tipo);
        Assert.True(errorPago.Errores!.ContainsKey("resultado"));

        var paginacionValidator = provider.GetRequiredService<IValidator<PaginacionQuery>>();
        Assert.NotNull(await paginacionValidator.ValidarAsync(new PaginacionQuery(0, 150)));
        Assert.Null(await paginacionValidator.ValidarAsync(new PaginacionQuery(1, 20)));

        var consumoQueryValidator = provider.GetRequiredService<IValidator<ConsultarConsumoQuery>>();
        Assert.Null(await consumoQueryValidator.ValidarAsync(new ConsultarConsumoQuery(null)));
        Assert.Null(await consumoQueryValidator.ValidarAsync(new ConsultarConsumoQuery("2026-11")));
        Assert.NotNull(await consumoQueryValidator.ValidarAsync(new ConsultarConsumoQuery("2026-13")));

        var crearInicialValidator = provider.GetRequiredService<IValidator<CrearSuscripcionInicialRequest>>();
        Assert.NotNull(await crearInicialValidator.ValidarAsync(
            new CrearSuscripcionInicialRequest(Guid.Empty, Guid.Empty, DateTime.Now)));

        var reservarValidator = provider.GetRequiredService<IValidator<ReservarConsumoRequest>>();
        Assert.NotNull(await reservarValidator.ValidarAsync(
            new ReservarConsumoRequest(Guid.NewGuid(), Guid.NewGuid(), 0m)));

        var usoTecnicoValidator = provider.GetRequiredService<IValidator<RegistrarUsoTecnicoRequest>>();
        Assert.NotNull(await usoTecnicoValidator.ValidarAsync(
            new RegistrarUsoTecnicoRequest(Guid.NewGuid(), Guid.NewGuid(), -1, 0, 0, -0.1m)));

        var cambiarValidator = provider.GetRequiredService<IValidator<CambiarSuscripcionCommand>>();
        var errorCambio = await cambiarValidator.ValidarAsync(
            new CambiarSuscripcionCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new CambiarSuscripcionDto(
                    Guid.NewGuid(),
                    "EstadoInvalido",
                    DateTime.UtcNow.AddDays(10),
                    false,
                    "corto")));

        Assert.NotNull(errorCambio);
        Assert.True(errorCambio.Errores!.ContainsKey("datos.estado"));
        Assert.True(errorCambio.Errores!.ContainsKey("datos.motivo"));
    }
}
