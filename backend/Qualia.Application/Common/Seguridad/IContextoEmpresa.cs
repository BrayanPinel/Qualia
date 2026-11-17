namespace Qualia.Application.Common.Seguridad;

/// <summary>
/// Empresa con la que trabaja el DbContext en la unidad de trabajo actual.
/// Los Global Query Filters usan <see cref="EmpresaIdActual"/>.
/// En una petición HTTP se toma del token del usuario.
/// En el worker (sin HttpContext) se establece explícitamente desde el trabajo persistido.
/// Si no hay empresa, los filtros no devuelven ninguna fila (falla cerrada).
/// </summary>
public interface IContextoEmpresa
{
    Guid? EmpresaIdActual { get; }

    /// <summary>
    /// Fija la empresa de esta unidad de trabajo (worker, URL firmada de audio).
    /// No permite sustituir la empresa de un usuario autenticado por otra distinta.
    /// </summary>
    void Establecer(Guid empresaId);
}
