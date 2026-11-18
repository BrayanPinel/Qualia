using Qualia.Domain.Servicios;

namespace Qualia.Tests.Unitarias;

public sealed class NormalizadorIdentidadTests
{
    [Theory]
    [InlineData("  Ana.Admin@Norte.DEMO ", "ana.admin@norte.demo")]
    [InlineData("ana.admin@norte.demo", "ana.admin@norte.demo")]
    [InlineData("\tJOSE@EMPRESA.COM\n", "jose@empresa.com")]
    public void Correo_se_recorta_y_pasa_a_minusculas(string entrada, string esperado)
    {
        Assert.Equal(esperado, NormalizadorIdentidad.Correo(entrada));
    }

    [Theory]
    [InlineData(" Demo-NORTE ", "demo-norte")]
    [InlineData("demo-sur", "demo-sur")]
    public void Codigo_se_recorta_y_pasa_a_minusculas(string entrada, string esperado)
    {
        Assert.Equal(esperado, NormalizadorIdentidad.Codigo(entrada));
    }

    [Fact]
    public void Acentos_compuestos_y_precompuestos_quedan_iguales()
    {
        var precompuesto = "josé@empresa.com";   // é en un solo carácter
        var compuesto = "josé@empresa.com";       // e + acento combinable

        Assert.Equal(NormalizadorIdentidad.Correo(precompuesto), NormalizadorIdentidad.Correo(compuesto));
    }

    [Fact]
    public void Normalizar_es_idempotente()
    {
        var una = NormalizadorIdentidad.Correo("  Ana@Norte.Demo ");

        Assert.Equal(una, NormalizadorIdentidad.Correo(una));
    }

    [Fact]
    public void Nulo_se_rechaza()
    {
        Assert.Throws<ArgumentNullException>(() => NormalizadorIdentidad.Correo(null!));
        Assert.Throws<ArgumentNullException>(() => NormalizadorIdentidad.Codigo(null!));
    }
}
