namespace Qualia.Domain.Enums;

// Enumeraciones definidas.
// Se almacenan como texto (varchar) en PostgreSQL y se validan con CHECK.

public enum RolEmpresa
{
    AdminEmpresa,
    Supervisor,
    Agente,
    Auditor
}

public enum EstadoEmpresa
{
    Activa,
    Suspendida
}

public enum EstadoRubrica
{
    Borrador,
    Activa,
    Retirada
}

public enum EstadoLlamada
{
    Disponible,
    EnCola,
    Transcribiendo,
    Transcrita,
    Evaluando,
    EvaluadaPorIA,
    Fallida
}

public enum EstadoTrabajo
{
    Pendiente,
    EnProceso,
    ReintentoPendiente,
    Completado,
    Fallido
}

public enum EstadoEvaluacion
{
    EvaluadaPorIA,
    ConfirmadaPorSupervisor,
    AjustadaPorSupervisor
}

public enum ModoIA
{
    Real,
    Simulado
}
