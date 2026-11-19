namespace Qualia.Domain.Common;

/// Marca las entidades que pertenecen a una empresa (tenant).
/// La infraestructura aplica automáticamente un Global Query Filter por EmpresaId
/// a todas las entidades que implementan esta interfaz.
public interface IEntidadEmpresa
{
    Guid EmpresaId { get; }
}
