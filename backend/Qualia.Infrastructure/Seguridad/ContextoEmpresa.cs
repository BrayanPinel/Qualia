using Qualia.Application.Common.Seguridad;

namespace Qualia.Infrastructure.Seguridad;

/// <summary>
/// Implementación scoped de <see cref="IContextoEmpresa"/>:
/// usa la empresa establecida explícitamente (worker) o, si no hay, la del usuario autenticado.
/// </summary>
public sealed class ContextoEmpresa(IUsuarioActual usuarioActual) : IContextoEmpresa
{
    private Guid? _establecida;

    public Guid? EmpresaIdActual => _establecida ?? usuarioActual.EmpresaId;

    public void Establecer(Guid empresaId)
    {
        if (empresaId == Guid.Empty)
        {
            throw new ArgumentException("La empresa no puede ser vacía.", nameof(empresaId));
        }

        var actual = EmpresaIdActual;
        if (actual is not null && actual != empresaId)
        {
            throw new InvalidOperationException("No se puede cambiar la empresa de una unidad de trabajo ya asociada a otra empresa.");
        }

        _establecida = empresaId;
    }
}
