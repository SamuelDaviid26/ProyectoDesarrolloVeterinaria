import 'dart:typed_data';

import 'package:flutter_file_dialog/flutter_file_dialog.dart';

import '../models/modelos.dart';
import 'api_client.dart';

/// Consultas del dueño sobre sus mascotas (api/mis-mascotas).
/// La API solo devuelve las mascotas del dueño que inició sesión (según su token).
class MascotasService {
  final ApiClient _api = ApiClient.instancia;

  Future<List<Mascota>> misMascotas() async {
    final lista = await _api.getJson('/api/mis-mascotas') as List;
    return lista
        .map((m) => Mascota.desdeJson(m as Map<String, dynamic>))
        .toList();
  }

  Future<List<Vacuna>> vacunas(int mascotaId) async {
    final lista =
        await _api.getJson('/api/mis-mascotas/$mascotaId/vacunas') as List;
    return lista
        .map((v) => Vacuna.desdeJson(v as Map<String, dynamic>))
        .toList();
  }

  Future<List<Examen>> examenes(int mascotaId) async {
    final lista =
        await _api.getJson('/api/mis-mascotas/$mascotaId/examenes') as List;
    return lista
        .map((e) => Examen.desdeJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<Tratamientos> tratamientos(int mascotaId) async {
    final json =
        await _api.getJson('/api/mis-mascotas/$mascotaId/tratamientos');
    return Tratamientos.desdeJson(json as Map<String, dynamic>);
  }

  /// Resumen para la pantalla de Inicio: cada mascota con sus vacunas y
  /// tratamientos. Las consultas de cada mascota se hacen en paralelo.
  Future<List<ResumenMascota>> resumen() async {
    final mascotas = await misMascotas();
    return Future.wait(mascotas.map((m) async {
      final datos = await Future.wait([vacunas(m.id), tratamientos(m.id)]);
      return ResumenMascota(
        mascota: m,
        vacunas: datos[0] as List<Vacuna>,
        tratamientos: datos[1] as Tratamientos,
      );
    }));
  }

  /// Historial clínico, de la nota más reciente a la más antigua.
  Future<List<Nota>> notas(int mascotaId) async {
    final lista =
        await _api.getJson('/api/mis-mascotas/$mascotaId/notas') as List;
    final notas =
        lista.map((n) => Nota.desdeJson(n as Map<String, dynamic>)).toList();
    notas.sort((a, b) => b.registradoEn.compareTo(a.registradoEn));
    return notas;
  }

  /// Trae el archivo del examen (PDF o imagen) a la memoria, para verlo
  /// dentro de la app sin guardarlo en el celular.
  Future<Uint8List> archivoExamen(Examen examen) async =>
      Uint8List.fromList(await _api.getBytes(examen.archivoUrl));

  /// Descarga el examen y abre el diálogo "Guardar como" de Android para que
  /// el dueño elija dónde guardarlo (por ejemplo, Descargas). No necesita
  /// permisos de almacenamiento. Devuelve null si el dueño cancela.
  Future<String?> guardarExamen(Examen examen, {Uint8List? bytes}) async {
    final datos = bytes ?? await archivoExamen(examen);
    return FlutterFileDialog.saveFile(
      params: SaveFileDialogParams(
        data: datos,
        fileName: examen.archivoNombre,
        mimeTypesFilter: [examen.archivoTipo],
      ),
    );
  }
}
