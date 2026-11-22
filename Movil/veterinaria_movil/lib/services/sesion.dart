import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// Guarda el token JWT del dueño en el almacenamiento seguro del celular.
///
/// Es una sola instancia para toda la app (Sesion.instancia).
class Sesion {
  Sesion._();
  static final Sesion instancia = Sesion._();

  static const _claveToken = 'token';
  static const _claveExpira = 'expira';
  static const _claveCorreo = 'correo';
  static const _claveNombre = 'nombre';
  static const _claveCedula = 'cedula';

  final FlutterSecureStorage _almacen = const FlutterSecureStorage();

  String? token;
  DateTime? expiraEnUtc;

  /// Correo con el que se inició sesión, para mostrar de quién es la cuenta
  /// (el token de la API no trae el nombre ni el correo).
  String? correo;

  /// Nombre y cédula del dueño, solo si el login de la API los devuelve
  /// (hoy no los devuelve; ver README). Si faltan, la app usa el correo.
  String? nombre;
  String? cedula;

  /// Primer nombre para el saludo ("Hola, Charle"), si se conoce.
  String? get primerNombre => (nombre == null || nombre!.trim().isEmpty)
      ? null
      : nombre!.trim().split(' ').first;

  /// Hay sesión si existe token y todavía no venció.
  bool get activa =>
      token != null &&
      expiraEnUtc != null &&
      DateTime.now().toUtc().isBefore(expiraEnUtc!);

  /// Se llama al abrir la app: recupera la sesión guardada, si sigue vigente.
  Future<void> cargar() async {
    token = await _almacen.read(key: _claveToken);
    final expira = await _almacen.read(key: _claveExpira);
    expiraEnUtc = expira == null ? null : DateTime.tryParse(expira)?.toUtc();
    correo = await _almacen.read(key: _claveCorreo);
    nombre = await _almacen.read(key: _claveNombre);
    cedula = await _almacen.read(key: _claveCedula);
    if (!activa) await cerrar();
  }

  Future<void> guardar(String nuevoToken, DateTime expira,
      {required String correoUsuario,
      String? nombreUsuario,
      String? cedulaUsuario}) async {
    token = nuevoToken;
    expiraEnUtc = expira.toUtc();
    correo = correoUsuario;
    nombre = nombreUsuario;
    cedula = cedulaUsuario;
    await _almacen.write(key: _claveCorreo, value: correoUsuario);
    await _almacen.write(key: _claveNombre, value: nombreUsuario);
    await _almacen.write(key: _claveCedula, value: cedulaUsuario);
    await _almacen.write(key: _claveToken, value: nuevoToken);
    await _almacen.write(
        key: _claveExpira, value: expiraEnUtc!.toIso8601String());
  }

  Future<void> cerrar() async {
    token = null;
    expiraEnUtc = null;
    correo = null;
    nombre = null;
    cedula = null;
    await _almacen.delete(key: _claveCorreo);
    await _almacen.delete(key: _claveNombre);
    await _almacen.delete(key: _claveCedula);
    await _almacen.delete(key: _claveToken);
    await _almacen.delete(key: _claveExpira);
  }
}
