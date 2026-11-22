import 'dart:async';
import 'dart:convert';
import 'dart:io';

import 'package:http/http.dart' as http;

import '../config.dart';
import 'sesion.dart';

/// Error que las pantallas muestran tal cual: el mensaje ya viene en español
/// y explicado para el dueño. Ninguna petición deja que la app se cierre.
class ApiException implements Exception {
  ApiException(this.mensaje, {this.sesionExpirada = false});

  final String mensaje;
  final bool sesionExpirada;

  @override
  String toString() => mensaje;
}

/// Único punto de la app que habla con la API.
///
/// - Agrega el token JWT en el encabezado Authorization de cada petición.
/// - Traduce los errores de red (sin internet, servidor caído, tiempo de
///   espera) y de la API (401, 403, 404, 500...) a ApiException.
/// - Si la sesión venció (401 o token expirado), la cierra y avisa a la app
///   para que regrese al login.
class ApiClient {
  ApiClient._();
  static final ApiClient instancia = ApiClient._();

  static const Duration _tiempoEspera = Duration(seconds: 20);
  static const String mensajeSesionExpirada =
      'Su sesión expiró. Inicie sesión de nuevo.';

  final http.Client _http = http.Client();

  /// La asigna main.dart: lleva al usuario a la pantalla de login.
  void Function()? alExpirarSesion;

  Uri uri(String ruta) => Uri.parse('$apiUrl$ruta');

  /// Encabezados con el token, para peticiones y para Image.network.
  Map<String, String> encabezados({bool json = false}) {
    final token = Sesion.instancia.token;
    return {
      if (token != null) 'Authorization': 'Bearer $token',
      if (json) 'Content-Type': 'application/json',
      'Accept': 'application/json',
    };
  }

  Future<dynamic> getJson(String ruta) async {
    final respuesta = await _enviar(
      () => _http.get(uri(ruta), headers: encabezados()),
    );
    return _leerJson(respuesta);
  }

  Future<dynamic> postJson(String ruta, Map<String, dynamic> cuerpo,
      {bool conSesion = true}) async {
    final respuesta = await _enviar(
      () => _http.post(
        uri(ruta),
        headers: conSesion
            ? encabezados(json: true)
            : {
                'Content-Type': 'application/json',
                'Accept': 'application/json'
              },
        body: jsonEncode(cuerpo),
      ),
      conSesion: conSesion,
    );
    return _leerJson(respuesta);
  }

  /// Descarga un archivo (foto o examen) como bytes.
  Future<List<int>> getBytes(String ruta) async {
    final respuesta = await _enviar(
      () => _http.get(uri(ruta), headers: encabezados()),
      tiempoEspera: const Duration(seconds: 60),
    );
    return respuesta.bodyBytes;
  }

  Future<http.Response> _enviar(
    Future<http.Response> Function() peticion, {
    bool conSesion = true,
    Duration tiempoEspera = _tiempoEspera,
  }) async {
    if (conSesion && !Sesion.instancia.activa) {
      await _sesionExpirada();
    }

    final http.Response respuesta;
    try {
      respuesta = await peticion().timeout(tiempoEspera);
    } on TimeoutException {
      throw ApiException(
          'El servidor tardó demasiado en responder. Intente de nuevo.');
    } on SocketException {
      throw ApiException(
          'No hay conexión con el servidor. Revise su internet e intente de nuevo.');
    } on http.ClientException {
      throw ApiException(
          'No hay conexión con el servidor. Revise su internet e intente de nuevo.');
    } on HandshakeException {
      throw ApiException(
          'No se pudo establecer una conexión segura con el servidor.');
    }

    if (respuesta.statusCode >= 200 && respuesta.statusCode < 300) {
      return respuesta;
    }
    if (respuesta.statusCode == 401 && conSesion) {
      await _sesionExpirada();
    }
    throw ApiException(_mensajeDeError(respuesta));
  }

  Future<Never> _sesionExpirada() async {
    await Sesion.instancia.cerrar();
    alExpirarSesion?.call();
    throw ApiException(mensajeSesionExpirada, sesionExpirada: true);
  }

  dynamic _leerJson(http.Response respuesta) {
    if (respuesta.bodyBytes.isEmpty) return null;
    return jsonDecode(utf8.decode(respuesta.bodyBytes));
  }

  /// Usa el mensaje que manda la API ({mensaje}, {errores} o ProblemDetails);
  /// si no hay, uno según el código HTTP.
  String _mensajeDeError(http.Response respuesta) {
    try {
      final cuerpo = _leerJson(respuesta);
      if (cuerpo is Map) {
        if (cuerpo['mensaje'] is String) return cuerpo['mensaje'] as String;
        final errores = cuerpo['errores'] ?? cuerpo['errors'];
        final lista = _aplanar(errores);
        if (lista.isNotEmpty) return lista.join('\n');
        if (cuerpo['detail'] is String) return cuerpo['detail'] as String;
        if (cuerpo['title'] is String) return cuerpo['title'] as String;
      }
    } catch (_) {
      // El cuerpo no era JSON: se usa el mensaje genérico.
    }
    switch (respuesta.statusCode) {
      case 401:
        return 'Correo o contraseña incorrectos.';
      case 403:
        return 'No tiene permiso para ver esta información.';
      case 404:
        return 'No se encontró la información solicitada.';
      default:
        return respuesta.statusCode >= 500
            ? 'El servidor tuvo un problema. Intente más tarde.'
            : 'No se pudo completar la solicitud (${respuesta.statusCode}).';
    }
  }

  List<String> _aplanar(dynamic errores) {
    if (errores is String) return [errores];
    if (errores is List) return errores.expand(_aplanar).toList();
    if (errores is Map) return errores.values.expand(_aplanar).toList();
    return [];
  }
}
