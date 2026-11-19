namespace Qualia.Application.Common.Seguridad;

/// Hash y verificación de contraseñas de Usuario y Administrador.
/// La contraseña en texto plano nunca se guarda, se registra en logs ni se audita.
public interface IServicioHashContrasena
{
    /// Devuelve un hash con sal aleatoria; dos llamadas con la misma contraseña dan hashes distintos.
    string Hashear(string contrasena);

    /// Compara en tiempo constante. Un hash vacío o con formato no reconocido se trata como incorrecto,
    /// nunca como excepción, para que el login responda siempre el mismo 401 genérico.
    ResultadoVerificacionContrasena Verificar(string hash, string contrasena);
}

public enum ResultadoVerificacionContrasena
{
    Incorrecta,
    Correcta,

    /// Correcta, pero el hash usa parámetros antiguos: conviene guardar un hash nuevo.
    CorrectaRequiereRehash,
}
