import 'api_client.dart';
import 'sesion.dart';

/// Inicio de sesión y registro del dueño (api/auth).
class AuthService {
  final ApiClient _api = ApiClient.instancia;

  /// POST api/auth/login -> { token, expiraEnUtc, rol }
  /// (y nombreCompleto / cedula si el backend los incluye)
  /// La API ya rechaza a los usuarios bloqueados (no les da token).
  Future<void> iniciarSesion(String correo, String contrasena) async {
    final respuesta = await _api.postJson(
      '/api/auth/login',
      {'correo': correo.trim(), 'contrasena': contrasena},
      conSesion: false,
    ) as Map<String, dynamic>;

    if (respuesta['rol'] != 'Dueño') {
      throw ApiException(
          'Esta aplicación es para dueños de mascotas. El personal de la clínica usa la aplicación web.');
    }

    await Sesion.instancia.guardar(
      respuesta['token'] as String,
      DateTime.parse(respuesta['expiraEnUtc'] as String),
      correoUsuario: correo.trim(),
      // Si el backend algún día los agrega al login, se muestran en la app.
      nombreUsuario:
          (respuesta['nombreCompleto'] ?? respuesta['nombre']) as String?,
      cedulaUsuario: respuesta['cedula'] as String?,
    );
  }

  /// POST api/auth/registro: el dueño crea su propia cuenta.
  /// La cédula une la cuenta con las mascotas que registra la clínica.
  Future<void> registrarse({
    required String cedula,
    required String nombreCompleto,
    required String correo,
    required String contrasena,
  }) async {
    await _api.postJson(
      '/api/auth/registro',
      {
        'cedula': cedula,
        'nombreCompleto': nombreCompleto,
        'correo': correo,
        'contrasena': contrasena,
      },
      conSesion: false,
    );
  }

  Future<void> cerrarSesion() => Sesion.instancia.cerrar();
}
