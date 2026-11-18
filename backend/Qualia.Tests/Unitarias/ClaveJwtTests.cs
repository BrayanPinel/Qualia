using Qualia.API.Seguridad;
using Qualia.Application.Common.Options;

namespace Qualia.Tests.Unitarias;

public sealed class ClaveJwtTests
{
    [Fact]
    public void Una_clave_configurada_de_32_o_mas_caracteres_se_usa_tal_cual()
    {
        var clave = ClaveJwt.Crear(new JwtOptions { Key = new string('k', 32) }, esDesarrollo: false);

        Assert.False(clave.EsTemporal);
        Assert.Equal(32, clave.Clave.Key.Length);
    }

    [Fact]
    public void Una_clave_corta_se_rechaza_en_cualquier_entorno()
    {
        Assert.Throws<InvalidOperationException>(() => ClaveJwt.Crear(new JwtOptions { Key = "corta" }, esDesarrollo: true));
    }

    [Fact]
    public void Sin_clave_fuera_de_desarrollo_la_api_no_arranca()
    {
        Assert.Throws<InvalidOperationException>(() => ClaveJwt.Crear(new JwtOptions(), esDesarrollo: false));
    }

    [Fact]
    public void Sin_clave_en_desarrollo_se_genera_una_temporal_distinta_cada_vez()
    {
        var una = ClaveJwt.Crear(new JwtOptions(), esDesarrollo: true);
        var otra = ClaveJwt.Crear(new JwtOptions(), esDesarrollo: true);

        Assert.True(una.EsTemporal);
        Assert.NotEqual(una.Clave.Key, otra.Clave.Key);
    }
}
