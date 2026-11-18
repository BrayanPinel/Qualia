using FluentValidation;
using Qualia.Application.Common.Models;
using Qualia.Domain.Suscripciones;

namespace Qualia.Application.Features.Desempeno.Dtos;

public sealed record ScorecardDto(
    Guid EmpresaId,
    Guid AgenteId,
    Guid CampanaId,
    string Periodo,
    int LlamadasEvaluadas,
    int LlamadasConfirmadas,
    int LlamadasElegibles,
    bool MuestraSuficiente,
    decimal? PuntajePromedio,
    decimal? Tendencia,
    decimal? PromedioCampana,
    int CriticosFallidos,
    IReadOnlyList<PromedioCriterioDto> Criterios);

public sealed record PromedioCriterioDto(Guid RubricaId, int Version, Guid CriterioId, string Nombre, decimal Promedio, int Cantidad);

public sealed record ReporteCampanaDto(
    Guid EmpresaId,
    Guid CampanaId,
    string Periodo,
    int LlamadasElegibles,
    bool MuestraSuficiente,
    decimal? Promedio,
    int CriticosFallidos);

/// <summary>Query de los scorecards individuales. Sin periodo se usa el mes UTC en curso.</summary>
public sealed class FiltroScorecardDto
{
    public Guid? CampanaId { get; set; }
    public string? Periodo { get; set; }
}

/// <summary>Query de los scorecards de una campaña (paginados por agente).</summary>
public sealed class FiltroScorecardsCampanaDto : PaginacionDto
{
    public string? Periodo { get; set; }
}

/// <summary>Query del reporte agregado de una campaña.</summary>
public sealed class FiltroReporteDto
{
    public string? Periodo { get; set; }
}

internal static class ReglasPeriodo
{
    public const string Mensaje = "El periodo debe tener el formato YYYY-MM.";

    public static bool EsValido(string? periodo) => periodo is null || PeriodoMensual.TryParse(periodo, out _);
}

public sealed class FiltroScorecardValidator : AbstractValidator<FiltroScorecardDto>
{
    public FiltroScorecardValidator()
    {
        RuleFor(x => x.CampanaId).NotEmpty().WithMessage("La campaña es obligatoria.");
        RuleFor(x => x.Periodo).Must(ReglasPeriodo.EsValido).WithMessage(ReglasPeriodo.Mensaje);
    }
}

public sealed class FiltroScorecardsCampanaValidator : AbstractValidator<FiltroScorecardsCampanaDto>
{
    public FiltroScorecardsCampanaValidator()
    {
        Include(new PaginacionDtoValidator());
        RuleFor(x => x.Periodo).Must(ReglasPeriodo.EsValido).WithMessage(ReglasPeriodo.Mensaje);
    }
}

public sealed class FiltroReporteValidator : AbstractValidator<FiltroReporteDto>
{
    public FiltroReporteValidator()
    {
        RuleFor(x => x.Periodo).Must(ReglasPeriodo.EsValido).WithMessage(ReglasPeriodo.Mensaje);
    }
}
