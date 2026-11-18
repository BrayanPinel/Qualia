using FluentValidation;
using Qualia.Application.Features.Autenticacion.Dtos;

namespace Qualia.Application.Features.Autenticacion.Validadores;

// En el login solo se exige presencia y un tope de longitud: las reglas de formato del registro
// no se repiten para no revelar por qué falla un intento.

public sealed class LoginValidator : AbstractValidator<LoginDto>
{
    public LoginValidator()
    {
        RuleFor(x => x.CodigoEmpresa)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("El código de empresa es obligatorio.")
            .MaximumLength(40).WithMessage("El código admite como máximo 40 caracteres.");

        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("El email es obligatorio.")
            .MaximumLength(254).WithMessage("El email admite como máximo 254 caracteres.");

        RuleFor(x => x.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MaximumLength(128).WithMessage("La contraseña admite como máximo 128 caracteres.");
    }
}

public sealed class LoginPlataformaValidator : AbstractValidator<LoginPlataformaDto>
{
    public LoginPlataformaValidator()
    {
        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("El email es obligatorio.")
            .MaximumLength(254).WithMessage("El email admite como máximo 254 caracteres.");

        RuleFor(x => x.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MaximumLength(128).WithMessage("La contraseña admite como máximo 128 caracteres.");
    }
}
