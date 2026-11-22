import 'package:flutter/material.dart';

import 'screens/bienvenida_screen.dart';
import 'screens/login_screen.dart';
import 'screens/principal_screen.dart';
import 'services/api_client.dart';
import 'services/sesion.dart';
import 'tema.dart';

/// Permite navegar desde fuera de una pantalla (cuando la sesión expira).
final GlobalKey<NavigatorState> navegador = GlobalKey<NavigatorState>();

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await Sesion.instancia.cargar();

  // Si la API dice que la sesión expiró, se vuelve al login con un aviso.
  var redirigiendo = false;
  ApiClient.instancia.alExpirarSesion = () {
    if (redirigiendo) return;
    redirigiendo = true;
    navegador.currentState?.pushAndRemoveUntil(
      MaterialPageRoute(
        builder: (_) =>
            const LoginScreen(aviso: ApiClient.mensajeSesionExpirada),
      ),
      (_) => false,
    );
    // Varias peticiones pueden fallar a la vez: se redirige una sola vez.
    Future.delayed(const Duration(seconds: 2), () => redirigiendo = false);
  };

  runApp(const VeterinariaApp());
}

class VeterinariaApp extends StatelessWidget {
  const VeterinariaApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Mi Veterinaria',
      debugShowCheckedModeBanner: false,
      navigatorKey: navegador,
      theme: temaVeterinaria(),
      home: Sesion.instancia.activa
          ? const PrincipalScreen()
          : const BienvenidaScreen(),
    );
  }
}
