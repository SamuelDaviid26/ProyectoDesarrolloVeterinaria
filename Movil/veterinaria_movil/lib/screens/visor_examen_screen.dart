import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:pdfx/pdfx.dart';

import '../models/modelos.dart';
import '../services/api_client.dart';
import '../services/mascotas_service.dart';
import '../tema.dart';
import '../widgets/comunes.dart';

/// Muestra el examen dentro de la app, sin guardarlo en el celular:
/// el PDF con su visor (zoom con dos dedos) o la imagen con zoom.
/// Para guardarlo, el dueño usa «Descargar» en la tarjeta del examen.
class VisorExamenScreen extends StatefulWidget {
  const VisorExamenScreen({super.key, required this.examen});

  final Examen examen;

  @override
  State<VisorExamenScreen> createState() => _VisorExamenScreenState();
}

class _VisorExamenScreenState extends State<VisorExamenScreen> {
  final _servicio = MascotasService();
  late Future<Uint8List> _archivo = _servicio.archivoExamen(widget.examen);
  PdfControllerPinch? _pdf;

  @override
  void dispose() {
    _pdf?.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final examen = widget.examen;
    return Scaffold(
      backgroundColor: examen.esPdf ? const Color(0xFFE8ECF4) : Colors.black,
      appBar: AppBar(
        title: Text(examen.descripcion, overflow: TextOverflow.ellipsis),
      ),
      body: FutureBuilder<Uint8List>(
        future: _archivo,
        builder: (context, estado) {
          if (estado.connectionState != ConnectionState.done) {
            return const Center(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  CircularProgressIndicator(),
                  SizedBox(height: 16),
                  Text('Abriendo el examen…',
                      style: TextStyle(color: ColoresVet.textoSuave)),
                ],
              ),
            );
          }
          if (estado.hasError) {
            final error = estado.error;
            return MensajeEstado(
              icono: Icons.error_outline,
              titulo: 'No se pudo abrir el examen',
              mensaje: error is ApiException
                  ? error.mensaje
                  : 'Ocurrió un error inesperado. Intente de nuevo.',
              alReintentar: () => setState(
                  () => _archivo = _servicio.archivoExamen(widget.examen)),
            );
          }
          final bytes = estado.data!;
          if (examen.esPdf) {
            _pdf ??= PdfControllerPinch(document: PdfDocument.openData(bytes));
            return PdfViewPinch(controller: _pdf!);
          }
          // Imagen: zoom con dos dedos.
          return InteractiveViewer(
            maxScale: 5,
            child: Center(child: Image.memory(bytes, fit: BoxFit.contain)),
          );
        },
      ),
    );
  }
}
