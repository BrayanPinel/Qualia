using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Qualia.Application.Common.Errores;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Common.Resultados;
using Qualia.Application.Common.Seguridad;
using Qualia.Application.Features.Empresas.Dtos;
using Qualia.Domain.Entidades;
using Qualia.Domain.Enums;
using Qualia.Domain.Servicios;

namespace Qualia.Application.Features.Empresas;

/// <summary>
/// POST /api/empresas (HU-01): crea Empresa, su Usuario AdminEmpresa y la suscripción de prueba con su
/// periodo inicial en una sola transacción. Si algo falla no queda ningún registro parcial.
/// No crea un Administrador global de la plataforma.
/// </summary>
public sealed class RegistroEmpresa(
    IQualiaDbContext db,
    IValidator<RegistrarEmpresaDto> validador,
    IServicioHashContrasena hash,
    ICrearSuscripcionInicial suscripcionInicial,
    IRegistroAuditoria auditoria,
    TimeProvider reloj)
{
    public const string CodigoDuplicado = "CODIGO_EMPRESA_DUPLICADO";

    public async Task<EmpresaRegistradaDto> RegistrarAsync(RegistrarEmpresaDto dto, CancellationToken ct)
    {
        await validador.ValidarOLanzarAsync(dto, ct);

        var codigo = NormalizadorIdentidad.Codigo(dto.CodigoEmpresa!);
        if (await CodigoEnUsoAsync(codigo, ct))
        {
            throw Duplicado();
        }

        var ahora = reloj.GetUtcNow().UtcDateTime;
        var empresa = new Empresa
        {
            Codigo = codigo,
            Nombre = dto.NombreEmpresa!.Trim(),
            Estado = EstadoEmpresa.Activa,
            FechaRegistro = ahora,
        };
        var admin = new Usuario
        {
            EmpresaId = empresa.Id,
            Nombre = dto.NombreAdministrador!.Trim(),
            Correo = NormalizadorIdentidad.Correo(dto.Email!),
            Rol = RolEmpresa.AdminEmpresa,
            Activo = true,
            PasswordHash = hash.Hashear(dto.Password!),
        };

        await using var transaccion = await db.BeginTransactionAsync(ct);
        try
        {
            db.Empresas.Add(empresa);
            db.Usuarios.Add(admin);

            // Contrato de suscripciones: agrega suscripción de prueba y periodo sin guardar ni confirmar.
            var suscripcion = (await suscripcionInicial.CrearInicialAsync(empresa.Id, dto.PlanId!.Value, ahora, ct))
                .ValorOLanzar();

            await auditoria.RegistrarAsync(empresa.Id, admin.Id, "RegistrarEmpresa", "Empresa", empresa.Id,
                despues: new { empresa.Codigo, empresa.Nombre, PlanId = dto.PlanId, AdministradorId = admin.Id },
                cancellationToken: ct);

            await db.SaveChangesAsync(ct);
            await transaccion.CommitAsync(ct);

            return new EmpresaRegistradaDto(empresa.Id, empresa.Codigo, admin.Id, suscripcion);
        }
        catch (DbUpdateException)
        {
            // Otra petición registró el mismo código entre la comprobación y el guardado.
            await transaccion.RollbackAsync(CancellationToken.None);
            db.LimpiarSeguimiento();
            if (await CodigoEnUsoAsync(codigo, ct))
            {
                throw Duplicado();
            }

            throw;
        }
        catch
        {
            db.LimpiarSeguimiento();
            throw;
        }
    }

    private Task<bool> CodigoEnUsoAsync(string codigo, CancellationToken ct) =>
        db.Empresas.AsNoTracking().AnyAsync(e => e.Codigo == codigo, ct);

    private static ExcepcionNegocio Duplicado() =>
        ExcepcionNegocio.Conflicto(CodigoDuplicado, "Ya existe una empresa registrada con ese código.");
}
