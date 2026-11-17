using Qualia.Domain.Comun;
using Qualia.Domain.Enums;

namespace Qualia.Domain.Entidades;

/// <summary>Tabla global empresa</summary>
public class Empresa
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public EstadoEmpresa Estado { get; set; } = EstadoEmpresa.Activa;
    public DateTime FechaRegistro { get; set; }
    public int MinimoMuestra { get; set; } = 5;
}

/// <summary>Tabla usuario.</summary>
public class Usuario : IEntidadEmpresa
{
    public Guid EmpresaId { get; set; }
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public RolEmpresa Rol { get; set; }
    public bool Activo { get; set; } = true;
    public string PasswordHash { get; set; } = string.Empty;
}

/// <summary>Tabla campana.</summary>
public class Campana : IEntidadEmpresa
{
    public Guid EmpresaId { get; set; }
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
}

/// <summary>Tabla puente usuario_campana</summary>
public class UsuarioCampana : IEntidadEmpresa
{
    public Guid EmpresaId { get; set; }
    public Guid UsuarioId { get; set; }
    public Guid CampanaId { get; set; }
}

/// <summary>Tabla rubrica (versión de rúbrica de una campaña).</summary>
public class Rubrica : IEntidadEmpresa
{
    public Guid EmpresaId { get; set; }
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid CampanaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public EstadoRubrica Estado { get; set; } = EstadoRubrica.Borrador;
    public DateTime FechaCreacion { get; set; }

    public ICollection<CriterioRubrica> Criterios { get; set; } = [];

    /// <summary>Una rúbrica Activa o Retirada ya fue publicada y es inmutable (RN-04).</summary>
    public bool EstaPublicada => Estado != EstadoRubrica.Borrador;
}

/// <summary>Tabla criterio_rubrica</summary>
public class CriterioRubrica : IEntidadEmpresa
{
    public Guid EmpresaId { get; set; }
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid RubricaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public decimal Peso { get; set; }
    public bool EsCritico { get; set; }
    public decimal UmbralCumplimiento { get; set; }
}
