using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Qualia.API.Middleware;
using Qualia.Application.Common.Errores;

namespace Qualia.Tests.Unitarias;

public class ManejadorErroresMiddlewareTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task ValidationException_de_FluentValidation_se_responde_como_400_VALIDACION_por_campo()
    {
        var excepcion = new ValidationException(
        [
            new ValidationFailure("Nombre", "El nombre es obligatorio."),
            new ValidationFailure("Nombre", "El nombre es demasiado corto."),
            new ValidationFailure("Peso", "El peso debe ser positivo."),
        ]);

        var (status, error) = await EjecutarAsync(_ => throw excepcion);

        Assert.Equal(StatusCodes.Status400BadRequest, status);
        Assert.Equal(400, error.Status);
        Assert.Equal("VALIDACION", error.Codigo);
        Assert.NotNull(error.Errores);
        Assert.Equal(["El nombre es obligatorio.", "El nombre es demasiado corto."], error.Errores!["Nombre"]);
        Assert.Equal(["El peso debe ser positivo."], error.Errores["Peso"]);
        Assert.False(string.IsNullOrEmpty(error.TraceId));
    }

    [Fact]
    public async Task Excepcion_no_controlada_se_responde_como_500_sin_detalles_internos()
    {
        var (status, error) = await EjecutarAsync(_ => throw new InvalidOperationException("Password=secreta"));

        Assert.Equal(StatusCodes.Status500InternalServerError, status);
        Assert.Equal("ERROR_INTERNO", error.Codigo);
        Assert.DoesNotContain("secreta", error.Mensaje);
    }

    private static async Task<(int Status, ErrorDto Error)> EjecutarAsync(RequestDelegate siguiente)
    {
        var contexto = new DefaultHttpContext();
        contexto.Response.Body = new MemoryStream();

        var middleware = new ManejadorErroresMiddleware(siguiente, NullLogger<ManejadorErroresMiddleware>.Instance);
        await middleware.InvokeAsync(contexto);

        contexto.Response.Body.Position = 0;
        var error = await JsonSerializer.DeserializeAsync<ErrorDto>(contexto.Response.Body, Json);
        return (contexto.Response.StatusCode, error!);
    }
}
