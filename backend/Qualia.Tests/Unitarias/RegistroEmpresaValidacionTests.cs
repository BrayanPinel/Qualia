using Qualia.Application.Features.Empresas.Dtos;
using Qualia.Application.Features.Empresas.Validadores;
using Qualia.Infrastructure.Seguridad;

namespace Qualia.Tests.Unitarias;

public sealed class RegistroEmpresaValidacionTests
{
    private static readonly RegistrarEmpresaValidator Validador = new();

    private static RegistrarEmpresaDto Valido() => new()
    {
        CodigoEmpresa = "mi-empresa-01",
        NombreEmpresa = "Mi Empresa",
        PlanId = Guid.NewGuid(),
        NombreAdministrador = "Ana Admin",
        Email = "ana@empresa.com",
        Password = new string('x', 12),
    };

    [Fact]
    public void Un_registro_completo_es_valido()
    {
        Assert.True(Validador.Validate(Valido()).IsValid);
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("con espacio")]
    [InlineData("guion_bajo")]
    [InlineData("acentuación")]
    public void Codigos_fuera_de_contrato_se_rechazan(string codigo)
    {
        var dto = Valido();
        dto.CodigoEmpresa = codigo;

        Assert.Contains(Validador.Validate(dto).Errors, e => e.PropertyName == nameof(RegistrarEmpresaDto.CodigoEmpresa));
    }

    [Theory]
    [InlineData(40, true)]
    [InlineData(41, false)]
    public void El_codigo_admite_hasta_40_caracteres(int longitud, bool valido)
    {
        var dto = Valido();
        dto.CodigoEmpresa = new string('a', longitud);

        Assert.Equal(valido, Validador.Validate(dto).IsValid);
    }

    [Theory]
    [InlineData(11, false)]
    [InlineData(12, true)]
    [InlineData(128, true)]
    [InlineData(129, false)]
    public void La_contrasena_debe_tener_entre_12_y_128_caracteres(int longitud, bool valido)
    {
        var dto = Valido();
        dto.Password = new string('x', longitud);

        Assert.Equal(valido, Validador.Validate(dto).IsValid);
    }

    [Fact]
    public void Plan_vacio_se_rechaza()
    {
        var dto = Valido();
        dto.PlanId = Guid.Empty;

        Assert.False(Validador.Validate(dto).IsValid);
    }
}

public sealed class RegistroAuditoriaSerializacionTests
{
    [Fact]
    public void Quita_propiedades_sensibles_a_cualquier_profundidad()
    {
        var json = RegistroAuditoria.Serializar(new
        {
            Nombre = "Ana",
            Password = "secreta",
            PasswordHash = "AQAAAA",
            Detalle = new { AccessToken = "jwt", Correo = "ana@empresa.com", ApiKey = "k" },
            Lista = new[] { new { ClaveFirma = "x", Valor = 1 } },
        })!;

        Assert.Contains("Ana", json);
        Assert.Contains("ana@empresa.com", json);
        Assert.Contains("\"valor\":1", json);
        foreach (var prohibido in new[] { "secreta", "AQAAAA", "jwt", "\"k\"", "claveFirma", "password", "accessToken" })
        {
            Assert.DoesNotContain(prohibido, json, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Nulo_se_guarda_como_nulo()
    {
        Assert.Null(RegistroAuditoria.Serializar(null));
    }
}
