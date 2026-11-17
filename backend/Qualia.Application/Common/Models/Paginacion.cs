using FluentValidation;

namespace Qualia.Application.Common.Models;

/// <summary>PaginaDto&lt;T&gt; (fase1 §7.7).</summary>
public sealed record PaginaDto<T>(
    IReadOnlyList<T> Items,
    int Pagina,
    int TamanoPagina,
    int Total);

/// <summary>Parámetros estándar de paginación (fase1 §7.1).</summary>
public sealed record PaginacionQuery(int Pagina = 1, int TamanoPagina = 20);

/// <summary>Reglas de paginación con FluentValidation (fase1 §7.1).</summary>
public sealed class PaginacionQueryValidator : AbstractValidator<PaginacionQuery>
{
    public PaginacionQueryValidator()
    {
        RuleFor(x => x.Pagina)
            .GreaterThanOrEqualTo(1)
            .WithMessage("La página debe ser mayor o igual a 1.");

        RuleFor(x => x.TamanoPagina)
            .InclusiveBetween(1, 100)
            .WithMessage("El tamaño de página debe estar entre 1 y 100.");
    }
}
