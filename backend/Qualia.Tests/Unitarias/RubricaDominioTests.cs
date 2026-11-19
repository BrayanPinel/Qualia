using FluentValidation;
using Qualia.Application.Features.Campanas.Dtos;
using Qualia.Application.Features.Campanas.Validadores;
using Qualia.Application.Features.Revision.Dtos;
using Qualia.Application.Features.Revision.Validadores;
using Qualia.Domain.Common;
using Qualia.Domain.Entidades;
using Qualia.Domain.Enums;

namespace Qualia.Tests.Unitarias;

/// Reglas de rúbricas versionadas (RN-04, RN-05) y forma de las peticiones del Rol B.
public sealed class RubricaDominioTests
{
    private static CriterioRubrica Criterio(string nombre, decimal peso, decimal umbral = 70) =>
        new() { Nombre = nombre, Descripcion = "Descripción", Peso = peso, UmbralCumplimiento = umbral };

    [Fact]
    public void Publicar_un_borrador_valido_lo_deja_Activo()
    {
        var rubrica = new Rubrica { Criterios = [Criterio("Saludo", 40), Criterio("Cierre", 60)] };

        rubrica.Publicar();

        Assert.Equal(EstadoRubrica.Activa, rubrica.Estado);
        Assert.True(rubrica.EstaPublicada);
    }

    [Fact]
    public void Publicar_con_pesos_que_no_suman_100_falla()
    {
        var rubrica = new Rubrica { Criterios = [Criterio("Saludo", 40), Criterio("Cierre", 50)] };

        var ex = Assert.Throws<ReglaDominioException>(rubrica.Publicar);

        Assert.Equal("RUBRICA_INVALIDA", ex.Codigo);
        Assert.Contains("sumar exactamente 100", ex.Message);
        Assert.Equal(EstadoRubrica.Borrador, rubrica.Estado);
    }

    [Fact]
    public void Publicar_con_nombres_duplicados_falla_sin_distinguir_mayusculas()
    {
        var rubrica = new Rubrica { Criterios = [Criterio("Saludo", 50), Criterio(" saludo ", 50)] };

        var ex = Assert.Throws<ReglaDominioException>(rubrica.Publicar);

        Assert.Contains("duplicados", ex.Message);
    }

    [Theory]
    [InlineData(EstadoRubrica.Activa)]
    [InlineData(EstadoRubrica.Retirada)]
    public void Una_rubrica_publicada_es_inmutable(EstadoRubrica estado)
    {
        var rubrica = new Rubrica { Estado = estado, Criterios = [Criterio("Saludo", 100)] };

        Assert.Equal("RUBRICA_PUBLICADA", Assert.Throws<ReglaDominioException>(rubrica.AsegurarEditable).Codigo);
        Assert.Throws<ReglaDominioException>(rubrica.Publicar);
    }

    [Fact]
    public void Solo_una_rubrica_Activa_puede_retirarse()
    {
        var activa = new Rubrica { Estado = EstadoRubrica.Activa };
        activa.Retirar();
        Assert.Equal(EstadoRubrica.Retirada, activa.Estado);

        Assert.Throws<ReglaDominioException>(new Rubrica { Estado = EstadoRubrica.Borrador }.Retirar);
    }

    [Fact]
    public void Solo_Agente_y_Supervisor_son_asignables_a_campanas()
    {
        Assert.True(UsuarioCampana.RolAsignable(RolEmpresa.Agente));
        Assert.True(UsuarioCampana.RolAsignable(RolEmpresa.Supervisor));
        Assert.False(UsuarioCampana.RolAsignable(RolEmpresa.AdminEmpresa));
        Assert.False(UsuarioCampana.RolAsignable(RolEmpresa.Auditor));
    }

    [Fact]
    public void Validador_de_rubrica_rechaza_pesos_no_positivos_umbral_fuera_de_rango_y_duplicados()
    {
        var dto = new CrearRubricaDto
        {
            Nombre = "Calidad",
            Criterios =
            [
                new CriterioEntradaDto { Nombre = "Saludo", Descripcion = "d", Peso = 0, EsCritico = false, UmbralCumplimiento = 120 },
                new CriterioEntradaDto { Nombre = "SALUDO", Descripcion = "d", Peso = 10.555m, EsCritico = false, UmbralCumplimiento = 50 },
            ],
        };

        var resultado = new CrearRubricaValidator().Validate(dto);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.ErrorMessage.Contains("duplicados"));
        Assert.Contains(resultado.Errors, e => e.ErrorMessage.Contains("positivo"));
        Assert.Contains(resultado.Errors, e => e.ErrorMessage.Contains("umbral", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(resultado.Errors, e => e.ErrorMessage.Contains("dos decimales"));
    }

    [Fact]
    public void Validador_de_rubrica_permite_un_borrador_cuyos_pesos_aun_no_suman_100()
    {
        var dto = new CrearRubricaDto
        {
            Nombre = "Borrador en progreso",
            Criterios = [new CriterioEntradaDto { Nombre = "Saludo", Descripcion = "d", Peso = 30, EsCritico = false, UmbralCumplimiento = 70 }],
        };

        Assert.True(new CrearRubricaValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void Validador_de_ajuste_exige_motivo_justificacion_puntaje_y_criterios_unicos()
    {
        var criterio = Guid.NewGuid();
        var dto = new AjustarEvaluacionDto
        {
            RevisionEsperada = 0,
            Motivo = "corto",
            Criterios =
            [
                new AjusteCriterioDto { CriterioId = criterio, Puntaje = 101, Justificacion = "Justificación suficiente", Evidencias = [] },
                new AjusteCriterioDto { CriterioId = criterio, Puntaje = 50, Justificacion = "x", Evidencias = null },
            ],
        };

        var errores = new AjustarEvaluacionValidator().Validate(dto).Errors.Select(e => e.ErrorMessage).ToList();

        Assert.Contains(errores, e => e.Contains("revisión esperada"));
        Assert.Contains(errores, e => e.Contains("El motivo debe tener entre 10"));
        Assert.Contains(errores, e => e.Contains("exactamente una vez"));
        Assert.Contains(errores, e => e.Contains("entre 0 y 100"));
        Assert.Contains(errores, e => e.Contains("La justificación debe tener entre 10"));
        Assert.Contains(errores, e => e.Contains("lista vacía"));
    }
}
