import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../tema.dart';
import '../widgets/comunes.dart';
import 'login_screen.dart';
import 'registro_screen.dart';

/// Primera pantalla cuando no hay sesión: presenta la app y ofrece
/// "Iniciar sesión" o "Crear cuenta".
class BienvenidaScreen extends StatelessWidget {
  const BienvenidaScreen({super.key});

  Future<void> _crearCuenta(BuildContext context) async {
    final correo = await Navigator.of(context).push<String>(
      MaterialPageRoute(builder: (_) => const RegistroScreen()),
    );
    if (correo == null || !context.mounted) return;
    // Cuenta creada: directo al login con el correo ya escrito.
    Navigator.of(context).push(MaterialPageRoute(
      builder: (_) => LoginScreen(
        correoInicial: correo,
        exito: 'Su cuenta fue creada. Escriba su contraseña para entrar.',
      ),
    ));
  }

  @override
  Widget build(BuildContext context) {
    final textos = Theme.of(context).textTheme;
    return AnnotatedRegion<SystemUiOverlayStyle>(
      value: SystemUiOverlayStyle.light,
      child: Scaffold(
        body: Container(
          decoration: const BoxDecoration(gradient: ColoresVet.degradado),
          child: SafeArea(
            // Se puede desplazar si la pantalla es baja (tablet acostada).
            child: LayoutBuilder(
              builder: (context, medidas) => SingleChildScrollView(
                child: ConstrainedBox(
                  constraints: BoxConstraints(minHeight: medidas.maxHeight),
                  child: IntrinsicHeight(
                    child: AnchoMaximo(
                      ancho: 480,
                      child: Padding(
                        padding: const EdgeInsets.fromLTRB(28, 24, 28, 24),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.stretch,
                          children: [
                            const Spacer(flex: 2),
                            const Center(child: LogoVet(tamano: 120)),
                            const SizedBox(height: 28),
                            Text('Mi Veterinaria',
                                textAlign: TextAlign.center,
                                style: textos.displaySmall?.copyWith(
                                    color: Colors.white,
                                    fontWeight: FontWeight.bold)),
                            const SizedBox(height: 12),
                            Text(
                              'El expediente de sus mascotas, siempre con usted.',
                              textAlign: TextAlign.center,
                              style: textos.titleMedium
                                  ?.copyWith(color: const Color(0xEBEDE9FE)),
                            ),
                            const SizedBox(height: 28),
                            // Lo que puede hacer en la app.
                            for (final (icono, texto) in const [
                              (
                                Icons.vaccines_outlined,
                                'Vacunas y próximas dosis'
                              ),
                              (
                                Icons.description_outlined,
                                'Resultados de exámenes'
                              ),
                              (
                                Icons.medication_outlined,
                                'Tratamientos e historial'
                              ),
                            ])
                              Padding(
                                padding:
                                    const EdgeInsets.symmetric(vertical: 6),
                                child: Row(
                                  mainAxisAlignment: MainAxisAlignment.center,
                                  children: [
                                    Icon(icono, color: Colors.white, size: 22),
                                    const SizedBox(width: 10),
                                    Flexible(
                                      child: Text(texto,
                                          style: const TextStyle(
                                              color: Colors.white,
                                              fontSize: 15)),
                                    ),
                                  ],
                                ),
                              ),
                            const Spacer(flex: 3),
                            FilledButton.icon(
                              onPressed: () => Navigator.of(context).push(
                                  MaterialPageRoute(
                                      builder: (_) => const LoginScreen())),
                              style: FilledButton.styleFrom(
                                backgroundColor: Colors.white,
                                foregroundColor: ColoresVet.azulOscuro,
                                minimumSize: const Size.fromHeight(54),
                              ),
                              icon: const Icon(Icons.login),
                              label: const Text('Iniciar sesión'),
                            ),
                            const SizedBox(height: 12),
                            OutlinedButton.icon(
                              onPressed: () => _crearCuenta(context),
                              style: OutlinedButton.styleFrom(
                                foregroundColor: Colors.white,
                                side: const BorderSide(
                                    color: Colors.white, width: 1.5),
                                minimumSize: const Size.fromHeight(54),
                                textStyle: const TextStyle(
                                    fontWeight: FontWeight.w600, fontSize: 16),
                                shape: RoundedRectangleBorder(
                                    borderRadius: BorderRadius.circular(12)),
                              ),
                              icon: const Icon(Icons.person_add_alt_1_outlined),
                              label: const Text('Crear cuenta'),
                            ),
                            const SizedBox(height: 16),
                            const Text(
                              'Para dueños de mascotas atendidas en la clínica.',
                              textAlign: TextAlign.center,
                              style: TextStyle(
                                  color: Color(0xCCEDE9FE), fontSize: 13),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
