import 'package:flutter/material.dart';

import '../tema.dart';
import 'cuenta_screen.dart';
import 'inicio_screen.dart';
import 'mis_mascotas_screen.dart';

/// Pantalla principal después de iniciar sesión, con la barra de navegación
/// inferior: Inicio, Mis mascotas y Mi cuenta.
class PrincipalScreen extends StatefulWidget {
  const PrincipalScreen({super.key});

  @override
  State<PrincipalScreen> createState() => _PrincipalScreenState();
}

class _PrincipalScreenState extends State<PrincipalScreen> {
  int _pestana = 0;

  void _irA(int pestana) => setState(() => _pestana = pestana);

  @override
  Widget build(BuildContext context) {
    // El botón "atrás" de Android desde Mis mascotas o Mi cuenta vuelve a
    // Inicio; desde Inicio, sale de la app.
    return PopScope(
      canPop: _pestana == 0,
      onPopInvokedWithResult: (salio, _) {
        if (!salio) _irA(0);
      },
      child: Scaffold(
        // IndexedStack conserva cada pestaña (no se recarga al volver a ella).
        body: IndexedStack(
          index: _pestana,
          children: [
            InicioScreen(irA: _irA),
            MisMascotasScreen(alVolver: () => _irA(0)),
            CuentaScreen(alVolver: () => _irA(0)),
          ],
        ),
        bottomNavigationBar: NavigationBar(
          selectedIndex: _pestana,
          onDestinationSelected: _irA,
          backgroundColor: Colors.white,
          indicatorColor: ColoresVet.moradoSuave,
          surfaceTintColor: Colors.transparent,
          shadowColor: ColoresVet.azulOscuro,
          elevation: 8,
          destinations: const [
            NavigationDestination(
              icon: Icon(Icons.home_outlined),
              selectedIcon: Icon(Icons.home, color: ColoresVet.moradoMedio),
              label: 'Inicio',
            ),
            NavigationDestination(
              icon: Icon(Icons.pets_outlined),
              selectedIcon: Icon(Icons.pets, color: ColoresVet.moradoMedio),
              label: 'Mis mascotas',
            ),
            NavigationDestination(
              icon: Icon(Icons.person_outline),
              selectedIcon: Icon(Icons.person, color: ColoresVet.moradoMedio),
              label: 'Mi cuenta',
            ),
          ],
        ),
      ),
    );
  }
}
