import 'package:flutter/material.dart';

import '../services/api_client.dart';
import '../services/auth_service.dart';
import '../tema.dart';
import '../widgets/comunes.dart';
import 'principal_screen.dart';
import 'registro_screen.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key, this.aviso, this.exito, this.correoInicial});

  /// Correo ya escrito (al volver de crear la cuenta).
  final String? correoInicial;

  /// Aviso informativo al abrir (por ejemplo, "Su sesión expiró").
  final String? aviso;

  /// Mensaje de éxito al abrir (por ejemplo, "Cerró sesión").
  final String? exito;

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _formulario = GlobalKey<FormState>();
  late final _correo = TextEditingController(text: widget.correoInicial);
  final _contrasena = TextEditingController();
  final _auth = AuthService();

  bool _cargando = false;
  bool _ocultarContrasena = true;
  String? _error;
  late String? _aviso = widget.aviso;
  late String? _exito = widget.exito;

  @override
  void dispose() {
    _correo.dispose();
    _contrasena.dispose();
    super.dispose();
  }

  Future<void> _ingresar() async {
    if (!_formulario.currentState!.validate()) return;
    FocusScope.of(context).unfocus();
    setState(() {
      _cargando = true;
      _error = null;
      _aviso = null;
      _exito = null;
    });
    try {
      await _auth.iniciarSesion(_correo.text, _contrasena.text);
      if (!mounted) return;
      Navigator.of(context).pushReplacement(
        MaterialPageRoute(builder: (_) => const PrincipalScreen()),
      );
    } on ApiException catch (e) {
      setState(() => _error = e.mensaje);
    } catch (_) {
      setState(() => _error = 'Ocurrió un error inesperado. Intente de nuevo.');
    } finally {
      if (mounted) setState(() => _cargando = false);
    }
  }

  /// Al volver del registro con la cuenta creada, se deja el correo escrito.
  Future<void> _irARegistro() async {
    final correoCreado = await Navigator.of(context).push<String>(
      MaterialPageRoute(builder: (_) => const RegistroScreen()),
    );
    if (correoCreado == null || !mounted) return;
    setState(() {
      _correo.text = correoCreado;
      _contrasena.clear();
      _error = null;
      _aviso = null;
      _exito = 'Su cuenta fue creada. Escriba su contraseña para entrar.';
    });
  }

  @override
  Widget build(BuildContext context) {
    final puedeVolver = Navigator.of(context).canPop();
    return Scaffold(
      // Flecha para volver a la bienvenida (si se llegó desde ahí).
      appBar: puedeVolver
          ? AppBar(
              backgroundColor: Colors.transparent,
              foregroundColor: ColoresVet.azulOscuro,
              shape: const Border(),
              scrolledUnderElevation: 0,
            )
          : null,
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(20),
            child: AnchoMaximo(
              ancho: 440,
              child: Card(
                margin: EdgeInsets.zero,
                clipBehavior: Clip.antiAlias,
                elevation: 4,
                shadowColor: ColoresVet.azulOscuro.withValues(alpha: 0.25),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    const EncabezadoVet(
                      titulo: 'Mi Veterinaria',
                      subtitulo: 'El expediente de sus mascotas, en su celular',
                    ),
                    Padding(
                      padding: const EdgeInsets.fromLTRB(24, 24, 24, 20),
                      child: _formularioIngreso(),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }

  Widget _formularioIngreso() {
    final textos = Theme.of(context).textTheme;
    final colores = Theme.of(context).colorScheme;
    return AutofillGroup(
      child: Form(
        key: _formulario,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text('Iniciar sesión',
                style: textos.titleLarge?.copyWith(
                    color: ColoresVet.azulOscuro, fontWeight: FontWeight.bold)),
            const SizedBox(height: 4),
            Text('Use el correo y la contraseña de su cuenta de dueño.',
                style: TextStyle(color: colores.onSurfaceVariant)),
            const SizedBox(height: 20),
            if (_exito != null) ...[
              Aviso(texto: _exito!, tipo: TipoAviso.exito),
              const SizedBox(height: 16),
            ],
            if (_aviso != null) ...[
              Aviso(texto: _aviso!, tipo: TipoAviso.info),
              const SizedBox(height: 16),
            ],
            TextFormField(
              controller: _correo,
              keyboardType: TextInputType.emailAddress,
              autofillHints: const [AutofillHints.email],
              textInputAction: TextInputAction.next,
              decoration: const InputDecoration(
                labelText: 'Correo electrónico',
                hintText: 'nombre@correo.com',
                prefixIcon: Icon(Icons.email_outlined),
              ),
              validator: (valor) {
                final texto = valor?.trim() ?? '';
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
              obscureText: _ocultarContrasena,
              autofillHints: const [AutofillHints.password],
              textInputAction: TextInputAction.done,
              onFieldSubmitted: (_) => _cargando ? null : _ingresar(),
              decoration: InputDecoration(
                labelText: 'Contraseña',
                prefixIcon: const Icon(Icons.lock_outline),
                suffixIcon: IconButton(
                  tooltip: _ocultarContrasena
                      ? 'Mostrar contraseña'
                      : 'Ocultar contraseña',
                  icon: Icon(_ocultarContrasena
                      ? Icons.visibility_outlined
                      : Icons.visibility_off_outlined),
                  onPressed: () =>
                      setState(() => _ocultarContrasena = !_ocultarContrasena),
                ),
              ),
              validator: (valor) =>
                  (valor ?? '').isEmpty ? 'Escriba su contraseña.' : null,
            ),
            if (_error != null) ...[
              const SizedBox(height: 16),
              Aviso(texto: _error!),
            ],
            const SizedBox(height: 24),
            FilledButton.icon(
              onPressed: _cargando ? null : _ingresar,
              style: FilledButton.styleFrom(
                  minimumSize: const Size.fromHeight(52)),
              icon: _cargando
                  ? const SizedBox(
                      width: 20,
                      height: 20,
                      child: CircularProgressIndicator(
                          strokeWidth: 2.5, color: Colors.white),
                    )
                  : const Icon(Icons.login),
              label: Text(_cargando ? 'Entrando…' : 'Iniciar sesión'),
            ),
            const SizedBox(height: 20),
            Row(
              children: [
                const Expanded(child: Divider()),
                Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 12),
                  child: Text('¿Primera vez aquí?',
                      style: TextStyle(color: colores.onSurfaceVariant)),
                ),
                const Expanded(child: Divider()),
              ],
            ),
            const SizedBox(height: 12),
            OutlinedButton.icon(
              onPressed: _cargando ? null : _irARegistro,
              style: OutlinedButton.styleFrom(
                minimumSize: const Size.fromHeight(48),
                foregroundColor: ColoresVet.moradoMedio,
                side: const BorderSide(color: ColoresVet.moradoMedio),
                shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(12)),
              ),
              icon: const Icon(Icons.person_add_alt_1_outlined),
              label: const Text('Crear una cuenta'),
            ),
            const SizedBox(height: 16),
            Text(
              '¿Olvidó su contraseña? Comuníquese con la clínica.',
              textAlign: TextAlign.center,
              style: TextStyle(color: colores.onSurfaceVariant, fontSize: 13),
            ),
          ],
        ),
      ),
    );
  }
}
