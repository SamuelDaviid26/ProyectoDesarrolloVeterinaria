import 'package:flutter/material.dart';

import '../models/modelos.dart';
import '../services/mascotas_service.dart';
import '../tema.dart';
import '../widgets/comunes.dart';
import '../widgets/tarjeta_mascota.dart';
import 'examenes_screen.dart';
import 'historial_screen.dart';
import 'tratamientos_screen.dart';
import 'vacunas_screen.dart';

/// Menú de una mascota: vacunas y exámenes (obligatorios),
/// tratamientos e historial clínico (opcionales del enunciado).
class MenuMascotaScreen extends StatelessWidget {
  const MenuMascotaScreen({super.key, required this.mascota});

  final Mascota mascota;

  @override
  Widget build(BuildContext context) {
    final opciones = [
      _Opcion(Icons.vaccines_outlined, 'Vacunas', 'Aplicadas y próximas dosis',
          VacunasScreen(mascota: mascota),
          color: ColoresVet.moradoMedio,
          fondo: ColoresVet.moradoSuave,
          alerta: _AlertaVencidas(mascotaId: mascota.id)),
      _Opcion(Icons.description_outlined, 'Exámenes',
          'Ver y descargar resultados', ExamenesScreen(mascota: mascota),
          color: ColoresVet.azulMedio, fondo: const Color(0xFFE6EDF8)),
      _Opcion(Icons.medication_outlined, 'Tratamientos',
          'Vigentes y suspendidos', TratamientosScreen(mascota: mascota),
          color: ColoresVet.moradoBrillante, fondo: const Color(0xFFF3EEFC)),
      _Opcion(Icons.history_edu_outlined, 'Historial', 'Notas del veterinario',
          HistorialScreen(mascota: mascota),
          color: ColoresVet.azulOscuro, fondo: const Color(0xFFE8ECF4)),
    ];

    return Scaffold(
      appBar: AppBar(title: Text('Expediente de ${mascota.nombre}')),
      body: AnchoMaximo(
        child: ListView(
          padding: const EdgeInsets.only(bottom: 24),
          children: [
            // Foto grande con el nombre encima.
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 16, 16, 8),
              child: ClipRRect(
                borderRadius: BorderRadius.circular(20),
                child: Stack(
                  children: [
                    FotoBanner(fotoUrl: mascota.fotoUrl, alto: 230),
                    Positioned.fill(
                      child: DecoratedBox(
                        decoration: BoxDecoration(
                          gradient: LinearGradient(
                            begin: Alignment.topCenter,
                            end: Alignment.bottomCenter,
                            colors: [
                              Colors.transparent,
                              ColoresVet.azulOscuro.withValues(alpha: 0.85),
                            ],
                            stops: const [0.45, 1],
                          ),
                        ),
                      ),
                    ),
                    Positioned(
                      left: 18,
                      right: 18,
                      bottom: 16,
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(mascota.nombre,
                              style: Theme.of(context)
                                  .textTheme
                                  .headlineMedium
                                  ?.copyWith(
                                      color: Colors.white,
                                      fontWeight: FontWeight.bold)),
                          Text('${mascota.especie} · ${mascota.raza}',
                              style: const TextStyle(
                                  color: Color(0xEBEDE9FE), fontSize: 15)),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
            ),
            Padding(
              padding: const EdgeInsets.fromLTRB(20, 16, 20, 8),
              child: Semantics(
                header: true,
                child: Text('¿Qué desea consultar?',
                    style: Theme.of(context).textTheme.titleLarge?.copyWith(
                        color: ColoresVet.azulOscuro,
                        fontWeight: FontWeight.bold)),
              ),
            ),
            // Cuadros de 2 en 2 (4 por fila en tablet).
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 12),
              child: LayoutBuilder(builder: (context, medidas) {
                final columnas = medidas.maxWidth > 600 ? 4 : 2;
                return GridView.count(
                  crossAxisCount: columnas,
                  shrinkWrap: true,
                  physics: const NeverScrollableScrollPhysics(),
                  childAspectRatio: 1.05,
                  children: [
                    for (final o in opciones) _CuadroOpcion(opcion: o),
                  ],
                );
              }),
            ),
          ],
        ),
      ),
    );
  }
}

class _Opcion {
  const _Opcion(this.icono, this.titulo, this.detalle, this.destino,
      {required this.color, required this.fondo, this.alerta});

  final IconData icono;
  final String titulo;
  final String detalle;
  final Widget destino;
  final Color color;
  final Color fondo;
  final Widget? alerta;
}

/// Cuadro tocable de una sección del expediente.
class _CuadroOpcion extends StatelessWidget {
  const _CuadroOpcion({required this.opcion});

  final _Opcion opcion;

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.all(6),
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: () => Navigator.of(context)
            .push(MaterialPageRoute(builder: (_) => opcion.destino)),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Container(
                    padding: const EdgeInsets.all(12),
                    decoration: BoxDecoration(
                      color: opcion.fondo,
                      borderRadius: BorderRadius.circular(14),
                    ),
                    child: Icon(opcion.icono, color: opcion.color, size: 28),
                  ),
                  const Spacer(),
                  if (opcion.alerta != null) opcion.alerta!,
                ],
              ),
              const Spacer(),
              Text(opcion.titulo,
                  style: const TextStyle(
                      fontSize: 17,
                      fontWeight: FontWeight.bold,
                      color: ColoresVet.azulOscuro)),
              const SizedBox(height: 2),
              SizedBox(
                  height: 36,
                  child: Text(opcion.detalle,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(
                          color: ColoresVet.textoSuave, fontSize: 13))),
            ],
          ),
        ),
      ),
    );
  }
}

/// Avisa desde el menú si hay vacunas con la próxima dosis vencida,
/// sin tener que entrar a la pantalla de vacunas. Si falla, no muestra nada.
class _AlertaVencidas extends StatefulWidget {
  const _AlertaVencidas({required this.mascotaId});

  final int mascotaId;

  @override
  State<_AlertaVencidas> createState() => _AlertaVencidasState();
}

class _AlertaVencidasState extends State<_AlertaVencidas> {
  late final Future<List<Vacuna>> _vacunas =
      MascotasService().vacunas(widget.mascotaId);

  @override
  Widget build(BuildContext context) {
    return FutureBuilder<List<Vacuna>>(
      future: _vacunas,
      builder: (context, estado) {
        final vencidas = estado.data?.where((v) => v.vencida).length ?? 0;
        if (vencidas == 0) return const SizedBox.shrink();
        return Padding(
          padding: const EdgeInsets.only(right: 8),
          child: Etiqueta(
            texto: vencidas == 1 ? '1 vencida' : '$vencidas vencidas',
            fondo: ColoresVet.rojoFondo,
            color: ColoresVet.rojo,
          ),
        );
      },
    );
  }
}
