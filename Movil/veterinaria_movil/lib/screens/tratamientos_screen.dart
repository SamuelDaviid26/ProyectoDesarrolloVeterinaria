import 'package:flutter/material.dart';

import '../models/modelos.dart';
import '../services/mascotas_service.dart';
import '../widgets/comunes.dart';

/// Medicamentos y tratamientos, separados en vigentes y suspendidos (opcional).
class TratamientosScreen extends StatelessWidget {
  const TratamientosScreen({super.key, required this.mascota});

  final Mascota mascota;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Tratamientos de ${mascota.nombre}')),
      body: VistaCarga<Tratamientos>(
        mensajeCarga: 'Cargando tratamientos…',
        cargar: () => MascotasService().tratamientos(mascota.id),
        construir: (context, datos) => ListView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.symmetric(vertical: 12),
          children: [
            _Titulo('Vigentes', datos.vigentes.length),
            if (datos.vigentes.isEmpty)
              const _Vacio('No hay medicamentos ni tratamientos vigentes.'),
            for (final t in datos.vigentes) _TarjetaTratamiento(tratamiento: t),
            const SizedBox(height: 12),
            _Titulo('Suspendidos', datos.suspendidos.length),
            if (datos.suspendidos.isEmpty)
              const _Vacio('No hay medicamentos ni tratamientos suspendidos.'),
            for (final t in datos.suspendidos)
              _TarjetaTratamiento(tratamiento: t),
          ],
        ),
      ),
    );
  }
}

class _Titulo extends StatelessWidget {
  const _Titulo(this.texto, this.cantidad);

  final String texto;
  final int cantidad;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.fromLTRB(20, 8, 20, 4),
        child: Text('$texto ($cantidad)',
            style: Theme.of(context).textTheme.titleMedium?.copyWith(
                color: Theme.of(context).colorScheme.primary,
                fontWeight: FontWeight.bold)),
      );
}

class _Vacio extends StatelessWidget {
  const _Vacio(this.texto);

  final String texto;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 8),
        child: Text(texto,
            style: TextStyle(
                color: Theme.of(context).colorScheme.onSurfaceVariant)),
      );
}

class _TarjetaTratamiento extends StatelessWidget {
  const _TarjetaTratamiento({required this.tratamiento});

  final Tratamiento tratamiento;

  @override
  Widget build(BuildContext context) {
    final colores = Theme.of(context).colorScheme;
    final suspendido = tratamiento.suspendidoEn != null;
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(tratamiento.nombre,
                      style: Theme.of(context).textTheme.titleMedium?.copyWith(
                          decoration:
                              suspendido ? TextDecoration.lineThrough : null)),
                ),
                Etiqueta(
                  texto: tratamiento.tipo,
                  fondo: colores.secondaryContainer,
                  color: colores.onSecondaryContainer,
                ),
              ],
            ),
            if (tratamiento.descripcion.isNotEmpty) ...[
              const SizedBox(height: 6),
              Text(tratamiento.descripcion),
            ],
            const SizedBox(height: 8),
            Text(
              'Asignado el ${formatoFecha(tratamiento.asignadoEn)} por ${tratamiento.veterinario}',
              style: TextStyle(color: colores.onSurfaceVariant, fontSize: 13),
            ),
            if (suspendido)
              Text(
                'Suspendido el ${formatoFecha(tratamiento.suspendidoEn!)}'
                '${tratamiento.veterinarioSuspension == null ? '' : ' por ${tratamiento.veterinarioSuspension}'}',
                style: TextStyle(color: colores.error, fontSize: 13),
              ),
          ],
        ),
      ),
    );
  }
}
