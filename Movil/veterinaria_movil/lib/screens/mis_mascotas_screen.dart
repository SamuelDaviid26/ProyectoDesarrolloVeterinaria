import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../models/modelos.dart';
import '../services/mascotas_service.dart';
import '../tema.dart';
import '../widgets/comunes.dart';
import '../widgets/tarjeta_mascota.dart';

/// Pestaña Mis mascotas: todas las mascotas del dueño en tarjetas grandes,
/// con buscador por nombre y filtro por especie.
class MisMascotasScreen extends StatefulWidget {
  const MisMascotasScreen({super.key, this.alVolver});

  /// Vuelve a la pestaña de Inicio (flecha de regreso del encabezado).
  final VoidCallback? alVolver;

  @override
  State<MisMascotasScreen> createState() => _MisMascotasScreenState();
}

class _MisMascotasScreenState extends State<MisMascotasScreen> {
  final _carga = GlobalKey<VistaCargaState<List<ResumenMascota>>>();

  @override
  Widget build(BuildContext context) {
    return AnnotatedRegion<SystemUiOverlayStyle>(
      value: SystemUiOverlayStyle.dark,
      child: Scaffold(
        body: SafeArea(
          bottom: false,
          child: VistaCarga<List<ResumenMascota>>(
            key: _carga,
            cargar: MascotasService().resumen,
            mensajeCarga: 'Buscando sus mascotas…',
            construir: (context, resumen) => _Lista(
              resumen: resumen,
              alVolver: widget.alVolver,
              alActualizar: () => _carga.currentState?.recargar(),
            ),
          ),
        ),
      ),
    );
  }
}

class _Lista extends StatefulWidget {
  const _Lista(
      {required this.resumen, this.alVolver, required this.alActualizar});

  final List<ResumenMascota> resumen;
  final VoidCallback? alVolver;
  final VoidCallback alActualizar;

  @override
  State<_Lista> createState() => _ListaState();
}

class _ListaState extends State<_Lista> {
  String _busqueda = '';
  String? _especie;

  @override
  Widget build(BuildContext context) {
    final resumen = widget.resumen;
    final especies = {for (final r in resumen) r.mascota.especie}.toList()
      ..sort();
    final filtradas = filtrarMascotas(resumen, _busqueda, _especie);
    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.only(bottom: 24),
      children: [
        EncabezadoPestana(
          titulo: 'Mis mascotas',
          subtitulo: switch (resumen.length) {
            0 => 'Todavía no tiene mascotas registradas.',
            1 => '1 mascota · tóquela para ver su expediente',
            _ =>
              '${resumen.length} mascotas · toque una para ver su expediente',
          },
          alVolver: widget.alVolver,
          accion: BotonActualizar(alTocar: widget.alActualizar),
        ),
        const SizedBox(height: 8),
        if (resumen.isEmpty)
          const Card(
            child: Padding(
              padding: EdgeInsets.all(24),
              child: Text(
                'La clínica registra a sus mascotas con su número de cédula. '
                'Cuando lo haga, aparecerán aquí. '
                'Deslice hacia abajo para actualizar.',
                textAlign: TextAlign.center,
                style: TextStyle(color: ColoresVet.textoSuave, height: 1.4),
              ),
            ),
          )
        else ...[
          BuscadorMascotas(
            especies: especies,
            especie: _especie,
            alBuscar: (t) => setState(() => _busqueda = t),
            alElegirEspecie: (e) => setState(() => _especie = e),
          ),
          const SizedBox(height: 8),
          if (filtradas.isEmpty)
            const SinResultados()
          else
            for (final r in filtradas) TarjetaMascota(resumen: r),
        ],
      ],
    );
  }
}
