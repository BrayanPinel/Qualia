using FluentValidation;

namespace Qualia.Application.Common.Models;

/// <summary>Parámetros de paginación de los listados: pagina desde 1, tamanoPagina de 1 a 100 (fase1 §7.1).</summary>
public class PaginacionDto
{
    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 20;

    public int Saltar => (Pagina - 1) * TamanoPagina;
}

public sealed class PaginacionDtoValidator : AbstractValidator<PaginacionDto>
{
    public PaginacionDtoValidator()
    {
        RuleFor(x => x.Pagina)
            .GreaterThanOrEqualTo(1).WithMessage("La página debe ser mayor o igual a 1.");

        RuleFor(x => x.TamanoPagina)
            .InclusiveBetween(1, 100).WithMessage("El tamaño de página debe estar entre 1 y 100.");
    }
}
