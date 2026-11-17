using Qualia.Application.Common.Interfaces;

namespace Qualia.Infrastructure.Multitenancy;

/// <summary>
/// Implementación provisional de <see cref="IContextoEmpresa"/> para peticiones HTTP
/// y trabajadores en segundo plano mientras se integra la implementación definitiva
/// del Rol A. Permite fijar la empresa activa en el alcance actual (falla cerrada
/// cuando <see cref="EmpresaId"/> es <c>null</c>).
/// </summary>
public sealed class ContextoEmpresaAmbiente : IContextoEmpresa
{
    private static readonly AsyncLocal<Guid?> EmpresaAsyncLocal = new();
    private Guid? _empresaIdLocal;

    public Guid? EmpresaId => _empresaIdLocal ?? EmpresaAsyncLocal.Value;

    public void EstablecerEmpresa(Guid? empresaId)
    {
        var normalizado = empresaId is { } id && id != Guid.Empty ? id : (Guid?)null;
        _empresaIdLocal = normalizado;
        EmpresaAsyncLocal.Value = normalizado;
    }
}
