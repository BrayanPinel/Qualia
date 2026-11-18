using FluentValidation;
using Qualia.Application.Features.Empresas.Validadores;
using Qualia.Application.Features.Usuarios.Dtos;

namespace Qualia.Application.Features.Usuarios.Validadores;

// Límites del contrato: nombre de 3 a 100; email válido hasta 254; contraseña de 12 a 128.

public sealed class CrearUsuarioValidator : AbstractValidator<CrearUsuarioDto>
{
    public CrearUsuarioValidator()
    {
        RuleFor(x => x.Nombre)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .Must(n => n!.Trim().Length is >= 3 and <= 100).WithMessage("El nombre debe tener entre 3 y 100 caracteres.");

        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("El email es obligatorio.")
            .Must(e => e!.Trim().Length <= 254).WithMessage("El email admite como máximo 254 caracteres.")
            .EmailAddress().WithMessage("El email no es válido.");

        RuleFor(x => x.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .Length(RegistrarEmpresaValidator.MinimoContrasena, RegistrarEmpresaValidator.MaximoContrasena)
                .WithMessage($"La contraseña debe tener entre {RegistrarEmpresaValidator.MinimoContrasena} y {RegistrarEmpresaValidator.MaximoContrasena} caracteres.");

        RuleFor(x => x.Rol)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("El rol es obligatorio.")
            .IsInEnum().WithMessage("El rol no es válido.");
    }
}

public sealed class ActualizarUsuarioValidator : AbstractValidator<ActualizarUsuarioDto>
{
    public ActualizarUsuarioValidator()
    {
        RuleFor(x => x.Nombre)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .Must(n => n!.Trim().Length is >= 3 and <= 100).WithMessage("El nombre debe tener entre 3 y 100 caracteres.");

        RuleFor(x => x.Activo)
            .NotNull().WithMessage("Indique si el usuario está activo.");
    }
}
