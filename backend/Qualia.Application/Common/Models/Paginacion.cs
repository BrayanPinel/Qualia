using FluentValidation;

namespace Qualia.Application.Common.Models;

/// Parámetros estándar de paginación.
public sealed record PaginacionQuery(int Pagina = 1, int TamanoPagina = 20);

/// Reglas de paginación con FluentValidation.
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
