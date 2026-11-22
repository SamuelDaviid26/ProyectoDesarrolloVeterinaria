import 'package:flutter/material.dart';

import '../config.dart';
import '../models/modelos.dart';
import '../screens/menu_mascota_screen.dart';
import '../services/api_client.dart';
import '../tema.dart';

/// Foto de la mascota a lo ancho (para tarjetas y encabezados). Si no tiene
/// foto o no carga, muestra una huella sobre el color suave del tema.
class FotoBanner extends StatelessWidget {
  const FotoBanner({super.key, required this.fotoUrl, this.alto = 170});

  final String? fotoUrl;
  final double alto;

  @override
  Widget build(BuildContext context) {
    final sinFoto = Container(
      height: alto,
      width: double.infinity,
      color: ColoresVet.moradoSuave,
      child: const Icon(Icons.pets, size: 64, color: ColoresVet.moradoMedio),
    );
    if (fotoUrl == null) return sinFoto;
    return Image.network(
      '$apiUrl$fotoUrl',
      headers: ApiClient.instancia.encabezados(),
      height: alto,
      width: double.infinity,
      fit: BoxFit.cover,
      // Las fotos suelen tener la cara arriba: se recorta de abajo.
      alignment: const Alignment(0, -0.6),
      errorBuilder: (_, __, ___) => sinFoto,
    );
  }
}

/// Pastilla sobre la foto (especie, estado de vacunas).
class Pastilla extends StatelessWidget {
  const Pastilla({
    super.key,
    required this.texto,
    required this.icono,
    this.fondo = Colors.white,
    this.color = ColoresVet.azulOscuro,
  });

  final String texto;
  final IconData icono;
  final Color fondo;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
      decoration: BoxDecoration(
        color: fondo,
        borderRadius: BorderRadius.circular(20),
        boxShadow: const [
          BoxShadow(
              color: Color(0x22000000), blurRadius: 6, offset: Offset(0, 2))
        ],
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icono, size: 15, color: color),
          const SizedBox(width: 5),
          Text(texto,
              style: TextStyle(
                  color: color, fontSize: 12, fontWeight: FontWeight.w600)),
        ],
      ),
    );
  }
}

/// Estado de vacunas de una mascota, para mostrarlo sobre la foto.
/// No depende solo del color: cada estado tiene su ícono y su texto.
Pastilla estadoVacunas(ResumenMascota r) {
  final vencidas = r.vencidas.length;
  if (vencidas > 0) {
    return Pastilla(
      texto: vencidas == 1 ? '1 vacuna vencida' : '$vencidas vacunas vencidas',
      icono: Icons.warning_amber_rounded,
      fondo: ColoresVet.rojo,
      color: Colors.white,
    );
  }
  final proximas = r.proximas();
  if (proximas.isNotEmpty) {
    final dias =
        proximas.first.fechaProximaDosis!.difference(DateTime.now()).inDays;
    return Pastilla(
      texto: dias <= 0
          ? 'Dosis hoy'
          : dias == 1
              ? 'Dosis mañana'
              : 'Dosis en $dias días',
      icono: Icons.event_outlined,
      fondo: ColoresVet.azulMedio,
      color: Colors.white,
    );
  }
  return const Pastilla(
    texto: 'Vacunas al día',
    icono: Icons.verified_outlined,
    fondo: ColoresVet.moradoBrillante,
    color: Colors.white,
  );
}

/// Tarjeta grande de una mascota: foto, especie, estado de vacunas, datos
/// y el botón "Ver expediente".
class TarjetaMascota extends StatelessWidget {
  const TarjetaMascota({super.key, required this.resumen, this.margen});

  final ResumenMascota resumen;
  final EdgeInsetsGeometry? margen;

  void _abrir(BuildContext context) =>
      Navigator.of(context).push(MaterialPageRoute(
          builder: (_) => MenuMascotaScreen(mascota: resumen.mascota)));

  @override
  Widget build(BuildContext context) {
    final m = resumen.mascota;
    final vacunas = resumen.vacunas.length;
    final tratamientos = resumen.tratamientos.vigentes.length;
    return Card(
      margin: margen ?? const EdgeInsets.fromLTRB(16, 6, 16, 10),
      clipBehavior: Clip.antiAlias,
      elevation: 2,
      shadowColor: ColoresVet.azulOscuro.withValues(alpha: 0.15),
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(20),
        side: const BorderSide(color: ColoresVet.bordeTarjeta),
      ),
      child: InkWell(
        onTap: () => _abrir(context),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Stack(
              children: [
                FotoBanner(fotoUrl: m.fotoUrl),
                Positioned(
                  left: 12,
                  top: 12,
                  child: Pastilla(texto: m.especie, icono: Icons.pets),
                ),
                Positioned(left: 12, bottom: 12, child: estadoVacunas(resumen)),
              ],
            ),
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 14, 16, 16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(m.nombre,
                      style: Theme.of(context).textTheme.titleLarge?.copyWith(
                          fontWeight: FontWeight.bold,
                          color: ColoresVet.azulOscuro)),
                  const SizedBox(height: 2),
                  Text('${m.especie} · ${m.raza}',
                      style: const TextStyle(color: ColoresVet.textoSuave)),
                  const SizedBox(height: 12),
                  Wrap(
                    spacing: 16,
                    runSpacing: 6,
                    children: [
                      _Dato(
                          icono: Icons.vaccines_outlined,
                          texto:
                              vacunas == 1 ? '1 vacuna' : '$vacunas vacunas'),
                      _Dato(
                          icono: Icons.medication_outlined,
                          texto: tratamientos == 1
                              ? '1 tratamiento activo'
                              : '$tratamientos tratamientos activos'),
                    ],
                  ),
                  const SizedBox(height: 14),
                  FilledButton.icon(
                    onPressed: () => _abrir(context),
                    style: FilledButton.styleFrom(
                        minimumSize: const Size.fromHeight(48)),
                    icon: const Icon(Icons.folder_open_outlined),
                    label: const Text('Ver expediente'),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _Dato extends StatelessWidget {
  const _Dato({required this.icono, required this.texto});

  final IconData icono;
  final String texto;

  @override
  Widget build(BuildContext context) => Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icono, size: 18, color: ColoresVet.moradoMedio),
          const SizedBox(width: 5),
          Text(texto, style: const TextStyle(color: ColoresVet.textoOscuro)),
        ],
      );
}

/// Buscador por nombre y filtro por especie (chips), como en las apps
/// móviles.
class BuscadorMascotas extends StatelessWidget {
  const BuscadorMascotas({
    super.key,
    required this.especies,
    required this.especie,
    required this.alBuscar,
    required this.alElegirEspecie,
  });

  final List<String> especies;
  final String? especie;
  final ValueChanged<String> alBuscar;
  final ValueChanged<String?> alElegirEspecie;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16),
          child: TextField(
            onChanged: alBuscar,
            textInputAction: TextInputAction.search,
            decoration: InputDecoration(
              hintText: 'Busque a su mascota por nombre',
              prefixIcon: const Icon(Icons.search),
              fillColor: Colors.white,
              enabledBorder: OutlineInputBorder(
                borderRadius: BorderRadius.circular(16),
                borderSide: const BorderSide(color: ColoresVet.borde),
              ),
              focusedBorder: OutlineInputBorder(
                borderRadius: BorderRadius.circular(16),
                borderSide: const BorderSide(
                    color: ColoresVet.moradoBrillante, width: 2),
              ),
            ),
          ),
        ),
        if (especies.length > 1) ...[
          const SizedBox(height: 12),
          SizedBox(
            height: 40,
            child: ListView(
              scrollDirection: Axis.horizontal,
              padding: const EdgeInsets.symmetric(horizontal: 16),
              children: [
                for (final e in [null, ...especies])
                  Padding(
                    padding: const EdgeInsets.only(right: 8),
                    child: ChoiceChip(
                      label: Text(e ?? 'Todas'),
                      selected: especie == e,
                      onSelected: (_) => alElegirEspecie(e),
                      showCheckmark: false,
                      selectedColor: ColoresVet.azulOscuro,
                      backgroundColor: Colors.white,
                      labelStyle: TextStyle(
                        color:
                            especie == e ? Colors.white : ColoresVet.azulOscuro,
                        fontWeight: FontWeight.w600,
                      ),
                      shape: StadiumBorder(
                        side: BorderSide(
                            color: especie == e
                                ? ColoresVet.azulOscuro
                                : ColoresVet.borde),
                      ),
                    ),
                  ),
              ],
            ),
          ),
        ],
      ],
    );
  }
}

/// Filtra el resumen por texto de búsqueda y especie.
List<ResumenMascota> filtrarMascotas(
    List<ResumenMascota> lista, String busqueda, String? especie) {
  final texto = busqueda.trim().toLowerCase();
  return lista
      .where((r) =>
          (especie == null || r.mascota.especie == especie) &&
          (texto.isEmpty || r.mascota.nombre.toLowerCase().contains(texto)))
      .toList();
}

/// Mensaje cuando la búsqueda no encuentra mascotas.
class SinResultados extends StatelessWidget {
  const SinResultados({super.key});

  @override
  Widget build(BuildContext context) => const Padding(
        padding: EdgeInsets.all(32),
        child: Column(
          children: [
            Icon(Icons.search_off, size: 48, color: ColoresVet.textoSuave),
            SizedBox(height: 8),
            Text('Ninguna mascota coincide con la búsqueda.',
                textAlign: TextAlign.center,
                style: TextStyle(color: ColoresVet.textoSuave)),
          ],
        ),
      );
}
