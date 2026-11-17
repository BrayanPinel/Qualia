using Qualia.Infrastructure.Procesamiento;

namespace Qualia.Tests.Unitarias;

public class EnmascaradorDatosTests
{
    private readonly EnmascaradorDatos _enmascarador = new();

    [Theory]
    [InlineData("mi correo es cliente.demo@example.com gracias", "mi correo es [CORREO] gracias")]
    [InlineData("mi identidad es 0801-1990-12345.", "mi identidad es [IDENTIDAD].")]
    [InlineData("pagué con 4111 1111 1111 1111 ayer", "pagué con [TARJETA] ayer")]
    [InlineData("pagué con 4111-1111-1111-1111", "pagué con [TARJETA]")]
    [InlineData("llámeme al 9876-5432", "llámeme al [TELEFONO]")]
    [InlineData("llámeme al +504 9876 5432", "llámeme al [TELEFONO]")]
    public void Enmascara_datos_sensibles(string original, string esperado)
    {
        Assert.Equal(esperado, _enmascarador.Enmascarar(original));
    }

    [Fact]
    public void Conserva_texto_sin_datos_sensibles()
    {
        const string texto = "Buenos días, gracias por llamar. ¿En qué le puedo ayudar?";
        Assert.Equal(texto, _enmascarador.Enmascarar(texto));
    }

    [Fact]
    public void Enmascara_varios_datos_en_la_misma_frase()
    {
        var resultado = _enmascarador.Enmascarar("Sí, mi identidad es 0801-1990-12345 y mi teléfono es 9876-5432.");
        Assert.Equal("Sí, mi identidad es [IDENTIDAD] y mi teléfono es [TELEFONO].", resultado);
    }
}
