// Modelos de lo que devuelve la API en /api/mis-mascotas.
// Cada uno corresponde a un DTO del backend (BE/Veterinaria/DTOs).

DateTime? _fecha(dynamic valor) =>
    valor == null ? null : DateTime.tryParse(valor as String)?.toLocal();

/// MiMascotaResponse
class Mascota {
  Mascota({
    required this.id,
    required this.nombre,
    required this.especie,
    required this.raza,
    this.fotoUrl,
  });

  final int id;
  final String nombre;
  final String especie;
  final String raza;
  final String? fotoUrl;

  factory Mascota.desdeJson(Map<String, dynamic> json) => Mascota(
        id: json['id'] as int,
        nombre: json['nombre'] as String,
        especie: json['especie'] as String,
        raza: json['raza'] as String,
        fotoUrl: json['fotoUrl'] as String?,
      );
}

/// VacunaResponse
class Vacuna {
  Vacuna({
    required this.id,
    required this.nombre,
    required this.fechaAplicacion,
    this.fechaProximaDosis,
    this.estadoProximaDosis,
    required this.veterinario,
  });

  final int id;
  final String nombre;
  final DateTime fechaAplicacion;
  final DateTime? fechaProximaDosis;

  /// "Vencida", "Pendiente" o null (sin próxima dosis, o ya se aplicó un refuerzo).
  final String? estadoProximaDosis;
  final String veterinario;

  /// La API decide si está vencida (con la fecha de Costa Rica).
  bool get vencida => estadoProximaDosis == 'Vencida';

  factory Vacuna.desdeJson(Map<String, dynamic> json) => Vacuna(
        id: json['id'] as int,
        nombre: json['nombreVacuna'] as String,
        fechaAplicacion: DateTime.parse(json['fechaAplicacion'] as String),
        fechaProximaDosis: json['fechaProximaDosis'] == null
            ? null
            : DateTime.parse(json['fechaProximaDosis'] as String),
        estadoProximaDosis: json['estadoProximaDosis'] as String?,
        veterinario: json['veterinarioResponsableNombre'] as String,
      );
}

/// ExamenResponse
class Examen {
  Examen({
    required this.id,
    required this.descripcion,
    required this.registradoEn,
    required this.archivoNombre,
    required this.archivoTipo,
    required this.archivoUrl,
    required this.veterinario,
  });

  final int id;
  final String descripcion;
  final DateTime registradoEn;
  final String archivoNombre;
  final String archivoTipo;
  final String archivoUrl;
  final String veterinario;

  bool get esPdf => archivoTipo == 'application/pdf';

  factory Examen.desdeJson(Map<String, dynamic> json) => Examen(
        id: json['id'] as int,
        descripcion: json['descripcion'] as String,
        registradoEn: _fecha(json['registradoEn'])!,
        archivoNombre: json['archivoNombre'] as String,
        archivoTipo: json['archivoTipoContenido'] as String,
        archivoUrl: json['archivoUrl'] as String,
        veterinario: json['veterinarioResponsableNombre'] as String,
      );
}

/// TratamientoResponse
class Tratamiento {
  Tratamiento({
    required this.id,
    required this.nombre,
    required this.descripcion,
    required this.tipo,
    required this.asignadoEn,
    required this.veterinario,
    this.suspendidoEn,
    this.veterinarioSuspension,
  });

  final int id;
  final String nombre;
  final String descripcion;
  final String tipo;
  final DateTime asignadoEn;
  final String veterinario;
  final DateTime? suspendidoEn;
  final String? veterinarioSuspension;

  factory Tratamiento.desdeJson(Map<String, dynamic> json) => Tratamiento(
        id: json['id'] as int,
        nombre: json['nombre'] as String,
        descripcion: json['descripcion'] as String,
        tipo: json['tipo'] as String,
        asignadoEn: _fecha(json['asignadoEn'])!,
        veterinario: json['veterinarioResponsableNombre'] as String,
        suspendidoEn: _fecha(json['suspendidoEn']),
        veterinarioSuspension:
            json['veterinarioSuspensionResponsableNombre'] as String?,
      );
}

/// TratamientosResponse: la API ya los separa en vigentes y suspendidos.
class Tratamientos {
  Tratamientos(this.vigentes, this.suspendidos);

  final List<Tratamiento> vigentes;
  final List<Tratamiento> suspendidos;

  factory Tratamientos.desdeJson(Map<String, dynamic> json) => Tratamientos(
        (json['vigentes'] as List)
            .map((t) => Tratamiento.desdeJson(t as Map<String, dynamic>))
            .toList(),
        (json['suspendidos'] as List)
            .map((t) => Tratamiento.desdeJson(t as Map<String, dynamic>))
            .toList(),
      );
}

/// NotaResponse (historial clínico, texto enriquecido en HTML)
class Nota {
  Nota({
    required this.id,
    required this.contenidoHtml,
    required this.registradoEn,
    required this.veterinario,
  });

  final int id;
  final String contenidoHtml;
  final DateTime registradoEn;
  final String veterinario;

  factory Nota.desdeJson(Map<String, dynamic> json) => Nota(
        id: json['id'] as int,
        contenidoHtml: json['contenidoHtml'] as String,
        registradoEn: _fecha(json['registradoEn'])!,
        veterinario: json['veterinarioResponsableNombre'] as String,
      );
}

/// Lo que muestra la pantalla de Inicio de cada mascota (se arma en la app
/// con tres consultas a la API: mascota, vacunas y tratamientos).
class ResumenMascota {
  ResumenMascota({
    required this.mascota,
    required this.vacunas,
    required this.tratamientos,
  });

  final Mascota mascota;
  final List<Vacuna> vacunas;
  final Tratamientos tratamientos;

  List<Vacuna> get vencidas => vacunas.where((v) => v.vencida).toList();

  /// Próximas dosis pendientes dentro de [dias] días, de la más cercana a la
  /// más lejana.
  List<Vacuna> proximas({int dias = 60}) {
    final hoy = DateTime.now();
    final limite = hoy.add(Duration(days: dias));
    final lista = vacunas
        .where((v) =>
            v.estadoProximaDosis == 'Pendiente' &&
            v.fechaProximaDosis != null &&
            v.fechaProximaDosis!.isBefore(limite))
        .toList()
      ..sort((a, b) => a.fechaProximaDosis!.compareTo(b.fechaProximaDosis!));
    return lista;
  }
}
