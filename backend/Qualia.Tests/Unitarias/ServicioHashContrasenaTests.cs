using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Qualia.Application.Common.Seguridad;
using Qualia.Infrastructure;
using Qualia.Infrastructure.Seguridad;

namespace Qualia.Tests.Unitarias;

public sealed class ServicioHashContrasenaTests
{
    private const string Contrasena = "Contrasena-Segura-2026";

    private static ServicioHashContrasena Crear(int iteraciones = OpcionesHashContrasena.MinimoIteraciones) =>
        new(Options.Create(new OpcionesHashContrasena { Iteraciones = iteraciones }));

    [Fact]
    public void El_hash_no_contiene_la_contrasena_y_se_verifica()
    {
        var servicio = Crear();

        var hash = servicio.Hashear(Contrasena);

        Assert.DoesNotContain(Contrasena, hash);
        Assert.Equal(ResultadoVerificacionContrasena.Correcta, servicio.Verificar(hash, Contrasena));
    }

    [Fact]
    public void Contrasena_incorrecta_o_con_otra_capitalizacion_no_se_acepta()
    {
        var servicio = Crear();
        var hash = servicio.Hashear(Contrasena);

        Assert.Equal(ResultadoVerificacionContrasena.Incorrecta, servicio.Verificar(hash, "Otra-Contrasena-2026"));
        Assert.Equal(ResultadoVerificacionContrasena.Incorrecta, servicio.Verificar(hash, Contrasena.ToUpperInvariant()));
    }

    [Fact]
    public void La_misma_contrasena_produce_hashes_distintos_por_la_sal()
    {
        var servicio = Crear();

        Assert.NotEqual(servicio.Hashear(Contrasena), servicio.Hashear(Contrasena));
    }

    [Fact]
    public void Contrasenas_largas_que_solo_difieren_despues_del_byte_72_son_distintas()
    {
        // BCrypt truncaría a 72 bytes y las consideraría iguales; el contrato admite hasta 128 caracteres.
        var servicio = Crear();
        var comun = new string('a', 100);
        var hash = servicio.Hashear(comun + "X");

        Assert.Equal(ResultadoVerificacionContrasena.Correcta, servicio.Verificar(hash, comun + "X"));
        Assert.Equal(ResultadoVerificacionContrasena.Incorrecta, servicio.Verificar(hash, comun + "Y"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("demo-sin-credenciales")]
    [InlineData("no-es-base64!!")]
    [InlineData("AQAAAAEAACcQAAAAEA==")]
    public void Un_hash_vacio_o_malformado_es_incorrecto_sin_lanzar(string hash)
    {
        Assert.Equal(ResultadoVerificacionContrasena.Incorrecta, Crear().Verificar(hash, Contrasena));
    }

    [Fact]
    public void Un_hash_con_menos_iteraciones_es_correcto_pero_pide_rehash()
    {
        var hashAntiguo = Crear(OpcionesHashContrasena.MinimoIteraciones).Hashear(Contrasena);

        var resultado = Crear(OpcionesHashContrasena.MinimoIteraciones * 2).Verificar(hashAntiguo, Contrasena);

        Assert.Equal(ResultadoVerificacionContrasena.CorrectaRequiereRehash, resultado);
    }

    [Fact]
    public void Contrasena_vacia_no_se_hashea()
    {
        Assert.ThrowsAny<ArgumentException>(() => Crear().Hashear(""));
    }

    [Fact]
    public void Iteraciones_por_debajo_del_minimo_se_rechazan()
    {
        Assert.Throws<InvalidOperationException>(() => Crear(OpcionesHashContrasena.MinimoIteraciones - 1));
    }

    [Fact]
    public void Infraestructura_registra_el_servicio_con_210000_iteraciones_por_defecto()
    {
        var configuracion = new ConfigurationBuilder().Build();
        using var proveedor = new ServiceCollection().AddInfrastructure(configuracion).BuildServiceProvider();

        var servicio = proveedor.GetRequiredService<IServicioHashContrasena>();
        var opciones = proveedor.GetRequiredService<IOptions<OpcionesHashContrasena>>().Value;

        Assert.IsType<ServicioHashContrasena>(servicio);
        Assert.Equal(210_000, opciones.Iteraciones);
    }
}
