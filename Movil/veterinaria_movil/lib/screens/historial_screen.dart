import 'package:flutter/material.dart';
import 'package:flutter_widget_from_html_core/flutter_widget_from_html_core.dart';

import '../models/modelos.dart';
import '../services/mascotas_service.dart';
import '../widgets/comunes.dart';

/// Historial clínico: notas en texto enriquecido (HTML), de la más reciente
/// a la más antigua (opcional).
class HistorialScreen extends StatelessWidget {
  const HistorialScreen({super.key, required this.mascota});

  final Mascota mascota;

  @override
  Widget build(BuildContext context) {
    final colores = Theme.of(context).colorScheme;
    return Scaffold(
      appBar: AppBar(title: Text('Historial de ${mascota.nombre}')),
      body: VistaCarga<List<Nota>>(
        mensajeCarga: 'Cargando historial…',
        cargar: () => MascotasService().notas(mascota.id),
        construir: (context, notas) {
          if (notas.isEmpty) {
            return const MensajeEstado(
              icono: Icons.history_edu_outlined,
              titulo: 'Sin notas',
              mensaje: 'El historial clínico todavía no tiene notas.',
            );
          }
          return ListView.builder(
            physics: const AlwaysScrollableScrollPhysics(),
            padding: const EdgeInsets.symmetric(vertical: 12),
            itemCount: notas.length,
            itemBuilder: (context, i) {
              final nota = notas[i];
              return Card(
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          Icon(Icons.schedule,
                              size: 16, color: colores.primary),
                          const SizedBox(width: 6),
                          Text(formatoFechaHora(nota.registradoEn),
                              style: TextStyle(
                                  color: colores.primary,
                                  fontWeight: FontWeight.w600)),
                        ],
                      ),
                      const Divider(height: 20),
                      // El backend ya limpia el HTML (HtmlSanitizer).
                      HtmlWidget(nota.contenidoHtml),
                      const SizedBox(height: 10),
                      Text('Dr(a). ${nota.veterinario}',
                          style: TextStyle(
                              color: colores.onSurfaceVariant,
                              fontStyle: FontStyle.italic)),
                    ],
                  ),
                ),
              );
            },
          );
        },
      ),
    );
  }
}
