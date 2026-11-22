import 'package:flutter/material.dart';

import '../services/api_client.dart';
import '../services/auth_service.dart';
import '../widgets/comunes.dart';

/// El dueño crea su propia cuenta (el enunciado lo permite).
class RegistroScreen extends StatefulWidget {
  const RegistroScreen({super.key});

  @override
  State<RegistroScreen> createState() => _RegistroScreenState();
}

class _RegistroScreenState extends State<RegistroScreen> {
  final _formulario = GlobalKey<FormState>();
  final _cedula = TextEditingController();
  final _nombre = TextEditingController();
  final _correo = TextEditingController();
  final _contrasena = TextEditingController();
  final _confirmacion = TextEditingController();
  final _auth = AuthService();

  bool _cargando = false;
  String? _error;

  @override
  void dispose() {
    for (final c in [_cedula, _nombre, _correo, _contrasena, _confirmacion]) {
      c.dispose();
    }
    super.dispose();
  }

  Future<void> _registrar() async {
    if (!_formulario.currentState!.validate()) return;
    setState(() {
      _cargando = true;
      _error = null;
    });
    try {
      await _auth.registrarse(
        cedula: _cedula.text.trim(),
        nombreCompleto: _nombre.text.trim(),
        correo: _correo.text.trim(),
        contrasena: _contrasena.text,
      );
      if (!mounted) return;
      // Vuelve al login con el correo ya escrito y un mensaje de éxito.
      Navigator.of(context).pop(_correo.text.trim());
    } on ApiException catch (e) {
      setState(() => _error = e.mensaje);
    } catch (_) {
      setState(() => _error = 'Ocurrió un error inesperado. Intente de nuevo.');
    } finally {
      if (mounted) setState(() => _cargando = false);
    }
  }

  /// Mismas reglas que ASP.NET Identity aplica por defecto en la API
  /// (así el error sale en español antes de enviar).
  String? _validarContrasena(String? valor) {
    final v = valor ?? '';
    if (v.length < 6) return 'Use al menos 6 caracteres.';
    if (!v.contains(RegExp(r'[A-Z]'))) return 'Incluya al menos una mayúscula.';
    if (!v.contains(RegExp(r'[a-z]'))) return 'Incluya al menos una minúscula.';
    if (!v.contains(RegExp(r'[0-9]'))) return 'Incluya al menos un número.';
    if (!v.contains(RegExp(r'[^a-zA-Z0-9]'))) {
      return 'Incluya al menos un símbolo (por ejemplo ! o #).';
    }
    return null;
  }

  String? _obligatorio(String? valor, String mensaje) =>
      (valor ?? '').trim().isEmpty ? mensaje : null;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Crear cuenta')),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: AnchoMaximo(
            ancho: 480,
            child: Form(
              key: _formulario,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  const Aviso(
                    tipo: TipoAviso.info,
                    texto:
                        'Use la misma cédula que dio en la clínica. Así la app '
                        'encuentra a sus mascotas.',
                  ),
                  const SizedBox(height: 20),
                  TextFormField(
                    controller: _cedula,
                    textInputAction: TextInputAction.next,
                    decoration: const InputDecoration(
                      labelText: 'Cédula',
                      prefixIcon: Icon(Icons.badge_outlined),
                      helperText: 'Ejemplo: 112340567',
                    ),
                    validator: (v) => _obligatorio(v, 'Escriba su cédula.'),
                  ),
                  const SizedBox(height: 16),
                  TextFormField(
                    controller: _nombre,
                    textCapitalization: TextCapitalization.words,
                    textInputAction: TextInputAction.next,
                    decoration: const InputDecoration(
                      labelText: 'Nombre completo',
                      prefixIcon: Icon(Icons.person_outline),
                    ),
                    validator: (v) => _obligatorio(v, 'Escriba su nombre.'),
                  ),
                  const SizedBox(height: 16),
                  TextFormField(
                    controller: _correo,
                    keyboardType: TextInputType.emailAddress,
                    textInputAction: TextInputAction.next,
                    decoration: const InputDecoration(
                      labelText: 'Correo electrónico',
                      prefixIcon: Icon(Icons.email_outlined),
                    ),
                    validator: (v) {
                      final texto = v?.trim() ?? '';
                      if (texto.isEmpty) return 'Escriba su correo.';
                      if (!esCorreoValido(texto)) {
                        return 'El correo no es válido (ejemplo: nombre@correo.com).';
                      }
                      return null;
                    },
                  ),
                  const SizedBox(height: 16),
                  TextFormField(
                    controller: _contrasena,
                    obscureText: true,
                    textInputAction: TextInputAction.next,
                    decoration: const InputDecoration(
                      labelText: 'Contraseña',
                      prefixIcon: Icon(Icons.lock_outline),
                      helperText:
                          'Mínimo 6 caracteres, con mayúscula, minúscula, número y símbolo.',
                      helperMaxLines: 2,
                    ),
                    validator: _validarContrasena,
                  ),
                  const SizedBox(height: 16),
                  TextFormField(
                    controller: _confirmacion,
                    obscureText: true,
                    textInputAction: TextInputAction.done,
                    decoration: const InputDecoration(
                      labelText: 'Confirmar contraseña',
                      prefixIcon: Icon(Icons.lock_outline),
                    ),
                    validator: (v) => v != _contrasena.text
                        ? 'Las contraseñas no coinciden.'
                        : null,
                  ),
                  if (_error != null) ...[
                    const SizedBox(height: 16),
                    Aviso(texto: _error!),
                  ],
                  const SizedBox(height: 24),
                  FilledButton.icon(
                    onPressed: _cargando ? null : _registrar,
                    style: FilledButton.styleFrom(
                        minimumSize: const Size.fromHeight(52)),
                    icon: _cargando
                        ? const SizedBox(
                            width: 20,
                            height: 20,
                            child: CircularProgressIndicator(
                                strokeWidth: 2.5, color: Colors.white),
                          )
                        : const Icon(Icons.person_add_alt_1),
                    label: Text(_cargando ? 'Creando cuenta…' : 'Crear cuenta'),
                  ),
                  const SizedBox(height: 8),
                  TextButton(
                    onPressed:
                        _cargando ? null : () => Navigator.of(context).pop(),
                    child: const Text('Ya tengo cuenta, iniciar sesión'),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
