using FluentValidation;
using Qualia.Application.Features.Empresas.Dtos;

namespace Qualia.Application.Features.Empresas.Validadores;

// Límites del contrato: código de 3 a 40 caracteres alfanuméricos o guiones; nombres de 3 a 100;
// email válido hasta 254; contraseña de 12 a 128 caracteres.

public sealed class RegistrarEmpresaValidator : AbstractValidator<RegistrarEmpresaDto>
{
    public const int MinimoContrasena = 12;
    public const int MaximoContrasena = 128;

    public RegistrarEmpresaValidator()
    {
        RuleFor(x => x.CodigoEmpresa)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("El código de empresa es obligatorio.")
            .Must(c => c!.Trim().Length is >= 3 and <= 40).WithMessage("El código debe tener entre 3 y 40 caracteres.")
            .Must(c => c!.Trim().All(ch => char.IsAsciiLetterOrDigit(ch) || ch == '-'))
                .WithMessage("El código solo admite letras, números y guiones.");

        RuleFor(x => x.NombreEmpresa)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("El nombre de la empresa es obligatorio.")
            .Must(n => n!.Trim().Length is >= 3 and <= 100).WithMessage("El nombre debe tener entre 3 y 100 caracteres.");

        RuleFor(x => x.PlanId)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("El plan es obligatorio.")
            .NotEqual(Guid.Empty).WithMessage("El plan es obligatorio.");

        RuleFor(x => x.NombreAdministrador)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("El nombre del administrador es obligatorio.")
            .Must(n => n!.Trim().Length is >= 3 and <= 100).WithMessage("El nombre debe tener entre 3 y 100 caracteres.");

        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("El email es obligatorio.")
            .Must(e => e!.Trim().Length <= 254).WithMessage("El email admite como máximo 254 caracteres.")
            .EmailAddress().WithMessage("El email no es válido.");

        RuleFor(x => x.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .Length(MinimoContrasena, MaximoContrasena)
                .WithMessage($"La contraseña debe tener entre {MinimoContrasena} y {MaximoContrasena} caracteres.");
    }
}
