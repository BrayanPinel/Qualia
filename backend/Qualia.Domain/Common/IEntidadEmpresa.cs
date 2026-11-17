namespace Qualia.Domain.Common;

/// <summary>
/// Marca las entidades que pertenecen a una empresa (tenant).
/// La infraestructura aplica automáticamente un Global Query Filter por <see cref="EmpresaId"/>
/// a todas las entidades que implementan esta interfaz.
/// </summary>
public interface IEntidadEmpresa
{
    Guid EmpresaId { get; }
}
