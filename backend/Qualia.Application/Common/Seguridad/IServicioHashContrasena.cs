namespace Qualia.Application.Common.Seguridad;

/// <summary>
/// Hash y verificación de contraseñas de Usuario y Administrador.
/// La contraseña en texto plano nunca se guarda, se registra en logs ni se audita.
/// </summary>
public interface IServicioHashContrasena
{
    /// <summary>Devuelve un hash con sal aleatoria; dos llamadas con la misma contraseña dan hashes distintos.</summary>
    string Hashear(string contrasena);

    /// <summary>
    /// Compara en tiempo constante. Un hash vacío o con formato no reconocido se trata como incorrecto,
    /// nunca como excepción, para que el login responda siempre el mismo 401 genérico.
    /// </summary>
    ResultadoVerificacionContrasena Verificar(string hash, string contrasena);
}

public enum ResultadoVerificacionContrasena
{
    Incorrecta,
    Correcta,

    /// <summary>Correcta, pero el hash usa parámetros antiguos: conviene guardar un hash nuevo.</summary>
    CorrectaRequiereRehash,
}
