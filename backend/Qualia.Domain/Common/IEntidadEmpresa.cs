namespace Qualia.Domain.Common;

/// <summary>
/// Marca una entidad propia de una empresa (tabla empresarial con <c>empresa_id</c>).
/// <c>QualiaDbContext</c> aplica automáticamente el filtro global multiempresa
/// a toda entidad que implemente esta interfaz (fase1 §6.1 y RN-01).
/// Las entidades globales (Plan, Funcion_plan, Auditoria_plataforma,
/// Empresa, Administrador) no la implementan.
/// </summary>
public interface IEntidadEmpresa
{
    Guid EmpresaId { get; }
}
