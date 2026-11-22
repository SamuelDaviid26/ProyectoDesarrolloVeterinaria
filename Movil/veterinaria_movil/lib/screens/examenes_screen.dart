import 'package:flutter/material.dart';

import '../models/modelos.dart';
import '../services/api_client.dart';
import '../services/mascotas_service.dart';
import '../tema.dart';
import '../widgets/comunes.dart';
import 'visor_examen_screen.dart';

/// Resultados de exámenes; cada archivo (PDF o imagen) se descarga y se abre.
class ExamenesScreen extends StatelessWidget {
  const ExamenesScreen({super.key, required this.mascota});

  final Mascota mascota;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Exámenes de ${mascota.nombre}')),
      body: VistaCarga<List<Examen>>(
        mensajeCarga: 'Cargando exámenes…',
        cargar: () => MascotasService().examenes(mascota.id),
        construir: (context, examenes) {
          if (examenes.isEmpty) {
            return const MensajeEstado(
              icono: Icons.description_outlined,
              titulo: 'Sin exámenes',
              mensaje:
                  'Todavía no hay resultados de exámenes en el expediente.',
            );
          }
          return ListView(
            physics: const AlwaysScrollableScrollPhysics(),
            padding: const EdgeInsets.symmetric(vertical: 12),
            children: [for (final e in examenes) _TarjetaExamen(examen: e)],
          );
        },
      ),
    );
  }
}

class _TarjetaExamen extends StatefulWidget {
  const _TarjetaExamen({required this.examen});

  final Examen examen;

  @override
  State<_TarjetaExamen> createState() => _TarjetaExamenState();
}

class _TarjetaExamenState extends State<_TarjetaExamen> {
  bool _descargando = false;

  /// Abre el examen dentro de la app (no lo guarda en el celular).
  void _ver() => Navigator.of(context).push(MaterialPageRoute(
      builder: (_) => VisorExamenScreen(examen: widget.examen)));

  /// Guarda el examen donde el dueño elija (diálogo "Guardar como").
  Future<void> _descargar() async {
    setState(() => _descargando = true);
    try {
      final ruta = await MascotasService().guardarExamen(widget.examen);
      _avisar(ruta == null
          ? 'Descarga cancelada.'
          : 'Examen guardado en su celular.');
    } on ApiException catch (e) {
      _avisar(e.mensaje);
    } catch (_) {
      _avisar('No se pudo descargar el archivo. Intente de nuevo.');
    } finally {
      if (mounted) setState(() => _descargando = false);
    }
  }

  void _avisar(String mensaje) {
    if (!mounted) return;
    ScaffoldMessenger.of(context)
        .showSnackBar(SnackBar(content: Text(mensaje)));
  }

  @override
  Widget build(BuildContext context) {
    final examen = widget.examen;
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Container(
                  padding: const EdgeInsets.all(10),
                  decoration: BoxDecoration(
                    color: ColoresVet.moradoSuave,
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: Icon(
                    examen.esPdf
                        ? Icons.picture_as_pdf_outlined
                        : Icons.image_outlined,
                    color: ColoresVet.moradoMedio,
                    size: 28,
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(examen.descripcion,
                          style: Theme.of(context)
                              .textTheme
                              .titleMedium
                              ?.copyWith(fontWeight: FontWeight.w600)),
                      const SizedBox(height: 4),
                      Text(
                        '${formatoFecha(examen.registradoEn)} · ${examen.veterinario}',
                        style: const TextStyle(color: ColoresVet.textoSuave),
                      ),
                      const SizedBox(height: 2),
                      Text(
                          '${examen.esPdf ? 'PDF' : 'Imagen'} · ${examen.archivoNombre}',
                          style: const TextStyle(
                              color: ColoresVet.textoSuave, fontSize: 12)),
                    ],
                  ),
                ),
              ],
            ),
            const SizedBox(height: 14),
            Row(
              children: [
                Expanded(
                  child: OutlinedButton.icon(
                    onPressed: _ver,
                    style: OutlinedButton.styleFrom(
                      minimumSize: const Size.fromHeight(48),
                      foregroundColor: ColoresVet.moradoMedio,
                      side: const BorderSide(color: ColoresVet.moradoMedio),
                      shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12)),
                    ),
                    icon: const Icon(Icons.visibility_outlined),
                    label: const Text('Ver'),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: FilledButton.icon(
                    onPressed: _descargando ? null : _descargar,
                    style: FilledButton.styleFrom(
                        minimumSize: const Size.fromHeight(48)),
                    icon: _descargando
                        ? const SizedBox(
                            width: 18,
                            height: 18,
                            child: CircularProgressIndicator(
                                strokeWidth: 2, color: Colors.white))
                        : const Icon(Icons.download),
                    label: Text(_descargando ? 'Descargando…' : 'Descargar'),
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
