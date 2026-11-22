import 'package:flutter/material.dart';

import '../models/modelos.dart';
import '../services/mascotas_service.dart';
import '../widgets/comunes.dart';

/// Vacunas aplicadas con la fecha de la próxima dosis; marca las vencidas.
class VacunasScreen extends StatelessWidget {
  const VacunasScreen({super.key, required this.mascota});

  final Mascota mascota;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Vacunas de ${mascota.nombre}')),
      body: VistaCarga<List<Vacuna>>(
        mensajeCarga: 'Cargando vacunas…',
        cargar: () => MascotasService().vacunas(mascota.id),
        construir: (context, vacunas) {
          if (vacunas.isEmpty) {
            return const MensajeEstado(
              icono: Icons.vaccines_outlined,
              titulo: 'Sin vacunas registradas',
              mensaje: 'Todavía no hay vacunas en el expediente.',
            );
          }
          final vencidas = vacunas.where((v) => v.vencida).length;
          return ListView(
            physics: const AlwaysScrollableScrollPhysics(),
            padding: const EdgeInsets.symmetric(vertical: 12),
            children: [
              if (vencidas > 0) _AvisoVencidas(cantidad: vencidas),
              for (final vacuna in vacunas) _TarjetaVacuna(vacuna: vacuna),
            ],
          );
        },
      ),
    );
  }
}

class _AvisoVencidas extends StatelessWidget {
  const _AvisoVencidas({required this.cantidad});

  final int cantidad;

  @override
  Widget build(BuildContext context) {
    final colores = Theme.of(context).colorScheme;
    return Card(
      color: colores.errorContainer,
      child: ListTile(
        leading:
            Icon(Icons.warning_amber_rounded, color: colores.onErrorContainer),
        title: Text(
          cantidad == 1
              ? 'Hay 1 vacuna con la próxima dosis vencida'
              : 'Hay $cantidad vacunas con la próxima dosis vencida',
          style: TextStyle(
              color: colores.onErrorContainer, fontWeight: FontWeight.w600),
        ),
        subtitle: Text('Comuníquese con la clínica para programarla.',
            style: TextStyle(color: colores.onErrorContainer)),
      ),
    );
  }
}

class _TarjetaVacuna extends StatelessWidget {
  const _TarjetaVacuna({required this.vacuna});

  final Vacuna vacuna;

  @override
  Widget build(BuildContext context) {
    final colores = Theme.of(context).colorScheme;
    final tema = Theme.of(context).textTheme;

    Widget? etiqueta;
    if (vacuna.vencida) {
      etiqueta = Etiqueta(
          texto: 'Vencida',
          fondo: colores.errorContainer,
          color: colores.onErrorContainer);
    } else if (vacuna.estadoProximaDosis == 'Pendiente') {
      etiqueta = Etiqueta(
          texto: 'Al día',
          fondo: colores.secondaryContainer,
          color: colores.onSecondaryContainer);
    } else if (vacuna.estadoProximaDosis == 'Cumplida') {
      // La API la marca así cuando ya se aplicó la dosis siguiente.
      etiqueta = Etiqueta(
          texto: 'Dosis siguiente aplicada',
          fondo: colores.primaryContainer,
          color: colores.onPrimaryContainer);
    }

    return Card(
      shape: vacuna.vencida
          ? RoundedRectangleBorder(
              side: BorderSide(color: colores.error, width: 1.5),
              borderRadius: BorderRadius.circular(12))
          : null,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(Icons.vaccines,
                    color: vacuna.vencida ? colores.error : colores.tertiary),
                const SizedBox(width: 8),
                Expanded(child: Text(vacuna.nombre, style: tema.titleMedium)),
                if (etiqueta != null) etiqueta,
              ],
            ),
            const SizedBox(height: 12),
            _Dato('Aplicada', formatoFecha(vacuna.fechaAplicacion)),
            _Dato(
              'Próxima dosis',
              vacuna.fechaProximaDosis == null
                  ? 'No requiere'
                  : formatoFecha(vacuna.fechaProximaDosis!),
              resaltar: vacuna.vencida,
            ),
            _Dato('Veterinario', vacuna.veterinario),
          ],
        ),
      ),
    );
  }
}

class _Dato extends StatelessWidget {
  const _Dato(this.titulo, this.valor, {this.resaltar = false});

  final String titulo;
  final String valor;
  final bool resaltar;

  @override
  Widget build(BuildContext context) {
    final colores = Theme.of(context).colorScheme;
    return Padding(
      padding: const EdgeInsets.only(top: 4),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 110,
            child:
                Text(titulo, style: TextStyle(color: colores.onSurfaceVariant)),
          ),
          Expanded(
            child: Text(valor,
                style: TextStyle(
                  fontWeight: resaltar ? FontWeight.bold : FontWeight.normal,
                  color: resaltar ? colores.error : null,
                )),
          ),
        ],
      ),
    );
  }
}
