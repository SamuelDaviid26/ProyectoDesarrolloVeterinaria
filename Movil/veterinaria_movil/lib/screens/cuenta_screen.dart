import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../models/modelos.dart';
import '../services/auth_service.dart';
import '../services/mascotas_service.dart';
import '../services/sesion.dart';
import '../tema.dart';
import '../widgets/comunes.dart';
import 'login_screen.dart';

/// Pestaña Mi cuenta: de quién es la sesión, ayuda y cerrar sesión.
class CuentaScreen extends StatelessWidget {
  const CuentaScreen({super.key, this.alVolver});

  /// Vuelve a la pestaña de Inicio (flecha de regreso del encabezado).
  final VoidCallback? alVolver;

  Future<void> _cerrarSesion(BuildContext context) async {
    final confirmar = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        icon: const Icon(Icons.logout, color: ColoresVet.moradoMedio),
        title: const Text('¿Cerrar sesión?'),
        content: const Text(
            'Para volver a ver el expediente de sus mascotas tendrá que '
            'escribir su correo y contraseña otra vez.'),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(context, false),
              child: const Text('Cancelar')),
          FilledButton(
              onPressed: () => Navigator.pop(context, true),
              child: const Text('Cerrar sesión')),
        ],
      ),
    );
    if (confirmar != true) return;
    await AuthService().cerrarSesion();
    if (!context.mounted) return;
    Navigator.of(context).pushAndRemoveUntil(
      MaterialPageRoute(
          builder: (_) =>
              const LoginScreen(exito: 'Cerró sesión correctamente.')),
      (_) => false,
    );
  }

  void _ayuda(BuildContext context, String titulo, String texto) {
    showDialog<void>(
      context: context,
      builder: (context) => AlertDialog(
        icon: const Icon(Icons.help_outline, color: ColoresVet.moradoMedio),
        title: Text(titulo),
        content: Text(texto),
        actions: [
          FilledButton(
              onPressed: () => Navigator.pop(context),
              child: const Text('Entendido')),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final sesion = Sesion.instancia;
    final correo = sesion.correo ?? 'Dueño de mascota';
    // Si el login de la API trae el nombre, se muestra; si no, el correo.
    final titulo = sesion.nombre ?? correo;
    final textos = Theme.of(context).textTheme;
    return AnnotatedRegion<SystemUiOverlayStyle>(
      value: SystemUiOverlayStyle.dark,
      child: Scaffold(
        body: SafeArea(
          bottom: false,
          child: AnchoMaximo(
            child: ListView(
              padding: const EdgeInsets.only(bottom: 24),
              children: [
                EncabezadoPestana(titulo: 'Mi cuenta', alVolver: alVolver),
                // Tarjeta de la cuenta
                Padding(
                  padding: const EdgeInsets.fromLTRB(16, 16, 16, 8),
                  child: Container(
                    padding: const EdgeInsets.all(20),
                    decoration: BoxDecoration(
                      gradient: ColoresVet.degradado,
                      borderRadius: BorderRadius.circular(20),
                    ),
                    child: Row(
                      children: [
                        CircleAvatar(
                          radius: 32,
                          backgroundColor: ColoresVet.moradoSuave,
                          child: Text(titulo.characters.first.toUpperCase(),
                              style: const TextStyle(
                                  fontSize: 28,
                                  fontWeight: FontWeight.bold,
                                  color: ColoresVet.moradoMedio)),
                        ),
                        const SizedBox(width: 16),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(titulo,
                                  style: textos.titleMedium?.copyWith(
                                      color: Colors.white,
                                      fontWeight: FontWeight.bold)),
                              if (sesion.nombre != null)
                                Text(correo,
                                    style: const TextStyle(
                                        color: Color(0xEBEDE9FE))),
                              if (sesion.cedula != null)
                                Text('Cédula ${sesion.cedula}',
                                    style: const TextStyle(
                                        color: Color(0xEBEDE9FE))),
                              const SizedBox(height: 6),
                              Container(
                                padding: const EdgeInsets.symmetric(
                                    horizontal: 10, vertical: 3),
                                decoration: BoxDecoration(
                                  color: Colors.white24,
                                  borderRadius: BorderRadius.circular(20),
                                ),
                                child: const Text('Cuenta de dueño',
                                    style: TextStyle(
                                        color: Colors.white, fontSize: 12)),
                              ),
                            ],
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
                const _ResumenCuenta(),
                const _Seccion('Ayuda'),
                _Opcion(
                  icono: Icons.pets_outlined,
                  titulo: '¿Cómo se agregan mis mascotas?',
                  alTocar: () => _ayuda(
                    context,
                    'Sus mascotas',
                    'La clínica registra a cada mascota y la asocia a su número '
                        'de cédula. Cuando lo hace, aparece automáticamente en '
                        'esta app con sus vacunas, exámenes, tratamientos e '
                        'historial.',
                  ),
                ),
                _Opcion(
                  icono: Icons.lock_reset_outlined,
                  titulo: '¿Olvidó o quiere cambiar su contraseña?',
                  alTocar: () => _ayuda(
                    context,
                    'Contraseña',
                    'Por seguridad, el cambio de contraseña lo hace la clínica. '
                        'Comuníquese con ella y le asignarán una nueva.',
                  ),
                ),
                _Opcion(
                  icono: Icons.file_download_outlined,
                  titulo: '¿Cómo veo o descargo un examen?',
                  alTocar: () => _ayuda(
                    context,
                    'Exámenes',
                    'Con «Ver» el examen se abre dentro de la app y no se guarda '
                        'en su celular.\n\nCon «Descargar» usted elige dónde '
                        'guardarlo (por ejemplo, en Descargas) para abrirlo o '
                        'compartirlo después.',
                  ),
                ),
                const _Seccion('Aplicación'),
                _Opcion(
                  icono: Icons.info_outline,
                  titulo: 'Acerca de Mi Veterinaria',
                  alTocar: () => showAboutDialog(
                    context: context,
                    applicationName: 'Mi Veterinaria',
                    applicationVersion: 'Versión 1.0.0',
                    applicationIcon: const LogoVet(tamano: 48),
                    children: const [
                      Text('Consulta del expediente clínico de sus mascotas: '
                          'vacunas, exámenes, tratamientos e historial.'),
                    ],
                  ),
                ),
                const SizedBox(height: 24),
                Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 16),
                  child: OutlinedButton.icon(
                    onPressed: () => _cerrarSesion(context),
                    style: OutlinedButton.styleFrom(
                      minimumSize: const Size.fromHeight(52),
                      foregroundColor: ColoresVet.rojo,
                      side: const BorderSide(color: ColoresVet.rojoBorde),
                      backgroundColor: ColoresVet.rojoFondo,
                      shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12)),
                    ),
                    icon: const Icon(Icons.logout),
                    label: const Text('Cerrar sesión',
                        style: TextStyle(fontWeight: FontWeight.w600)),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _Seccion extends StatelessWidget {
  const _Seccion(this.texto);

  final String texto;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.fromLTRB(20, 20, 20, 6),
        child: Semantics(
          header: true,
          child: Text(texto.toUpperCase(),
              style: const TextStyle(
                  color: ColoresVet.textoSuave,
                  fontSize: 12,
                  fontWeight: FontWeight.w700,
                  letterSpacing: 0.8)),
        ),
      );
}

class _Opcion extends StatelessWidget {
  const _Opcion(
      {required this.icono, required this.titulo, required this.alTocar});

  final IconData icono;
  final String titulo;
  final VoidCallback alTocar;

  @override
  Widget build(BuildContext context) => Card(
        clipBehavior: Clip.antiAlias,
        child: ListTile(
          leading: CircleAvatar(
            backgroundColor: ColoresVet.moradoSuave,
            child: Icon(icono, color: ColoresVet.moradoMedio),
          ),
          title:
              Text(titulo, style: const TextStyle(fontWeight: FontWeight.w500)),
          trailing:
              const Icon(Icons.chevron_right, color: ColoresVet.moradoMedio),
          onTap: alTocar,
        ),
      );
}

/// Resumen de las mascotas de la cuenta (2 x 2), como el recuadro de
/// "impacto" de las apps móviles. Si no carga, no se muestra.
class _ResumenCuenta extends StatefulWidget {
  const _ResumenCuenta();

  @override
  State<_ResumenCuenta> createState() => _ResumenCuentaState();
}

class _ResumenCuentaState extends State<_ResumenCuenta> {
  late final Future<List<ResumenMascota>> _resumen =
      MascotasService().resumen();

  @override
  Widget build(BuildContext context) {
    return FutureBuilder<List<ResumenMascota>>(
      future: _resumen,
      builder: (context, estado) {
        final r = estado.data;
        if (r == null) return const SizedBox.shrink();
        final vacunas = r.fold<int>(0, (s, m) => s + m.vacunas.length);
        final tratamientos =
            r.fold<int>(0, (s, m) => s + m.tratamientos.vigentes.length);
        final vencidas = r.fold<int>(0, (s, m) => s + m.vencidas.length);
        Widget cifra(int n, String texto, {bool alerta = false}) => Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('$n',
                    style: TextStyle(
                        fontSize: 26,
                        fontWeight: FontWeight.bold,
                        color:
                            alerta ? ColoresVet.rojo : ColoresVet.azulOscuro)),
                Text(texto,
                    style: const TextStyle(
                        color: ColoresVet.textoSuave, fontSize: 13)),
              ],
            );
        return Container(
          margin: const EdgeInsets.fromLTRB(16, 8, 16, 0),
          padding: const EdgeInsets.all(16),
          decoration: BoxDecoration(
            color: ColoresVet.moradoSuave,
            borderRadius: BorderRadius.circular(16),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Row(
                children: [
                  Icon(Icons.pets, color: ColoresVet.moradoMedio, size: 20),
                  SizedBox(width: 8),
                  Text('Sus mascotas en la clínica',
                      style: TextStyle(
                          color: ColoresVet.moradoMedio,
                          fontWeight: FontWeight.w600)),
                ],
              ),
              const SizedBox(height: 12),
              Row(
                children: [
                  Expanded(
                      child: cifra(
                          r.length, r.length == 1 ? 'mascota' : 'mascotas')),
                  Expanded(child: cifra(vacunas, 'vacunas registradas')),
                ],
              ),
              const SizedBox(height: 12),
              Row(
                children: [
                  Expanded(child: cifra(tratamientos, 'tratamientos activos')),
                  Expanded(
                      child: cifra(vencidas, 'vacunas vencidas',
                          alerta: vencidas > 0)),
                ],
              ),
            ],
          ),
        );
      },
    );
  }
}
