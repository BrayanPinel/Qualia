namespace Qualia.Application.Common.Interfaces;

/// <summary>
/// Contexto multiempresa de la operación en curso (RN-01).
/// En la API se obtiene del JWT validado; en el trabajador de procesamiento
/// debe obtenerse del trabajo en cola (fase1 §6.1). Nunca proviene del cliente.
/// <para>
/// <c>QualiaDbContext</c> lo usa para el filtro global de las entidades
/// <see cref="Qualia.Domain.Common.IEntidadEmpresa"/>. Si es <c>null</c>
/// las consultas empresariales filtradas no devuelven filas (falla cerrada).
/// </para>
/// <para>
/// PROVISIONAL (Rol C): introducido porque A06 aún no define el contexto de
/// sesión. Rol A puede reemplazar la implementación registrada sin cambiar
/// este contrato.
/// </para>
/// </summary>
public interface IContextoEmpresa
{
    Guid? EmpresaId { get; } 
}
