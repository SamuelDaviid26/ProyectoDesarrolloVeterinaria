import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../models/modelos.dart';
import '../services/mascotas_service.dart';
import '../services/sesion.dart';
import '../tema.dart';
import '../widgets/comunes.dart';
import '../widgets/tarjeta_mascota.dart';
import 'tratamientos_screen.dart';
import 'vacunas_screen.dart';

/// Pestaña de Inicio: saludo, campana de avisos, buscador, tarjetas de las
/// mascotas, avisos de vacunas y tratamientos vigentes. Todo sale de los
/// endpoints de api/mis-mascotas; la app solo lo junta y lo ordena.
class InicioScreen extends StatefulWidget {
  const InicioScreen({super.key, required this.irA});

  /// Cambia de pestaña en la barra inferior (1 = Mis mascotas).
  final void Function(int pestana) irA;

  @override
  State<InicioScreen> createState() => _InicioScreenState();
}

class _InicioScreenState extends State<InicioScreen> {
  final _carga = GlobalKey<VistaCargaState<List<ResumenMascota>>>();

  @override
  Widget build(BuildContext context) {
    // Sin barra de color arriba: íconos oscuros en la barra de estado.
    return AnnotatedRegion<SystemUiOverlayStyle>(
      value: SystemUiOverlayStyle.dark,
      child: Scaffold(
        body: SafeArea(
          bottom: false,
          child: VistaCarga<List<ResumenMascota>>(
            key: _carga,
            cargar: MascotasService().resumen,
            mensajeCarga: 'Preparando su resumen…',
            construir: (context, resumen) => _ContenidoInicio(
              resumen: resumen,
              irA: widget.irA,
              alActualizar: () => _carga.currentState?.recargar(),
            ),
          ),
        ),
      ),
    );
  }
}

class _ContenidoInicio extends StatefulWidget {
  const _ContenidoInicio(
      {required this.resumen, required this.irA, required this.alActualizar});

  final List<ResumenMascota> resumen;
  final void Function(int pestana) irA;
  final VoidCallback alActualizar;

  @override
  State<_ContenidoInicio> createState() => _ContenidoInicioState();
}

class _ContenidoInicioState extends State<_ContenidoInicio> {
  String _busqueda = '';
  String? _especie;

  @override
  Widget build(BuildContext context) {
    final resumen = widget.resumen;
    final especies = {for (final r in resumen) r.mascota.especie}.toList()
      ..sort();
    final filtradas = filtrarMascotas(resumen, _busqueda, _especie);
    final avisos = _avisos(context, resumen);
    final cantidadAvisos =
        avisos.where((a) => a.tipo != TipoAviso.exito).length;

    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.only(bottom: 24),
      children: [
        _Encabezado(
          cantidadAvisos: cantidadAvisos,
          alTocarCampana: () => _verAvisos(context, avisos),
        ),
        if (resumen.isEmpty)
          const _SinMascotas()
        else ...[
          BuscadorMascotas(
            especies: especies,
            especie: _especie,
            alBuscar: (t) => setState(() => _busqueda = t),
            alElegirEspecie: (e) => setState(() => _especie = e),
          ),
          _Titulo('Sus mascotas',
              accion: 'Ver todas',
              alTocar: () => widget.irA(1),
              extra: BotonActualizar(alTocar: widget.alActualizar)),
          if (filtradas.isEmpty)
            const SinResultados()
          else if (filtradas.length == 1)
            TarjetaMascota(resumen: filtradas.first)
          else
            // Varias mascotas: tarjetas que se deslizan de lado.
            SizedBox(
              height: 412,
              child: ListView.builder(
                scrollDirection: Axis.horizontal,
                padding: const EdgeInsets.symmetric(horizontal: 10),
                itemCount: filtradas.length,
                itemBuilder: (context, i) => SizedBox(
                  width: 300,
                  child: TarjetaMascota(
                    resumen: filtradas[i],
                    margen: const EdgeInsets.fromLTRB(6, 6, 6, 10),
                  ),
                ),
              ),
            ),
          const _Titulo('Tratamientos vigentes'),
          ..._tratamientos(context, resumen),
        ],
      ],
    );
  }

  /// Panel inferior con todos los avisos (al tocar la campana).
  void _verAvisos(BuildContext context, List<_TarjetaAviso> avisos) {
    showModalBottomSheet<void>(
      context: context,
      showDragHandle: true,
      backgroundColor: ColoresVet.fondo,
      builder: (_) => SafeArea(
        child: ListView(
          shrinkWrap: true,
          padding: const EdgeInsets.only(bottom: 16),
          children: [
            const Padding(
              padding: EdgeInsets.fromLTRB(20, 0, 20, 8),
              child: Text('Avisos',
                  style: TextStyle(
                      fontSize: 20,
                      fontWeight: FontWeight.bold,
                      color: ColoresVet.azulOscuro)),
            ),
            ...avisos,
          ],
        ),
      ),
    );
  }

  List<_TarjetaAviso> _avisos(
      BuildContext context, List<ResumenMascota> resumen) {
    final avisos = <_TarjetaAviso>[];
    for (final r in resumen) {
      for (final v in r.vencidas) {
        avisos.add(_TarjetaAviso(
          tipo: TipoAviso.error,
          icono: Icons.warning_amber_rounded,
          titulo: '${r.mascota.nombre} · ${v.nombre}',
          detalle: 'La próxima dosis venció el '
              '${formatoFecha(v.fechaProximaDosis!)}. Comuníquese con la clínica.',
          alTocar: () => _abrir(context, VacunasScreen(mascota: r.mascota)),
        ));
      }
    }
    for (final r in resumen) {
      for (final v in r.proximas()) {
        final dias = v.fechaProximaDosis!.difference(DateTime.now()).inDays;
        avisos.add(_TarjetaAviso(
          tipo: TipoAviso.info,
          icono: Icons.event_outlined,
          titulo: '${r.mascota.nombre} · ${v.nombre}',
          detalle: 'Próxima dosis el ${formatoFecha(v.fechaProximaDosis!)} '
              '(${dias <= 0 ? 'hoy' : dias == 1 ? 'mañana' : 'en $dias días'}).',
          alTocar: () => _abrir(context, VacunasScreen(mascota: r.mascota)),
        ));
      }
    }
    if (avisos.isEmpty) {
      avisos.add(const _TarjetaAviso(
        tipo: TipoAviso.exito,
        icono: Icons.verified_outlined,
        titulo: 'Vacunas al día',
        detalle: 'No hay dosis vencidas ni próximas en los siguientes 60 días.',
      ));
    }
    return avisos;
  }

  List<Widget> _tratamientos(
      BuildContext context, List<ResumenMascota> resumen) {
    final lista = <Widget>[
      for (final r in resumen)
        for (final t in r.tratamientos.vigentes)
          Card(
            clipBehavior: Clip.antiAlias,
            child: ListTile(
              leading: CircleAvatar(
                backgroundColor: ColoresVet.moradoSuave,
                child: Icon(
                    t.tipo == 'Medicamento'
                        ? Icons.medication_outlined
                        : Icons.healing_outlined,
                    color: ColoresVet.moradoMedio),
              ),
              title: Text(t.nombre,
                  style: const TextStyle(fontWeight: FontWeight.w600)),
              subtitle: Text('${r.mascota.nombre} · ${t.tipo}'),
              trailing: const Icon(Icons.chevron_right,
                  color: ColoresVet.moradoMedio),
              onTap: () =>
                  _abrir(context, TratamientosScreen(mascota: r.mascota)),
            ),
          ),
    ];
    if (lista.isEmpty) {
      lista.add(const _TarjetaAviso(
        tipo: TipoAviso.exito,
        icono: Icons.check_circle_outline,
        titulo: 'Sin tratamientos activos',
        detalle: 'Ninguna de sus mascotas tiene medicamentos o tratamientos '
            'vigentes.',
      ));
    }
    return lista;
  }

  void _abrir(BuildContext context, Widget pantalla) =>
      Navigator.of(context).push(MaterialPageRoute(builder: (_) => pantalla));
}

/// "Hola, Charle" (o "Hola" si la API no da el nombre), el correo y la
/// campana de avisos con el número de avisos pendientes.
class _Encabezado extends StatelessWidget {
  const _Encabezado(
      {required this.cantidadAvisos, required this.alTocarCampana});

  final int cantidadAvisos;
  final VoidCallback alTocarCampana;

  @override
  Widget build(BuildContext context) {
    final sesion = Sesion.instancia;
    final nombre = sesion.primerNombre;
    return Padding(
      padding: const EdgeInsets.fromLTRB(20, 16, 12, 16),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(nombre == null ? 'Hola' : 'Hola, $nombre',
                    style: Theme.of(context).textTheme.headlineMedium?.copyWith(
                        color: ColoresVet.azulOscuro,
                        fontWeight: FontWeight.bold)),
                if (sesion.correo != null)
                  Row(
                    children: [
                      const Icon(Icons.mail_outline,
                          size: 16, color: ColoresVet.textoSuave),
                      const SizedBox(width: 6),
                      Flexible(
                        child: Text(sesion.correo!,
                            overflow: TextOverflow.ellipsis,
                            style:
                                const TextStyle(color: ColoresVet.textoSuave)),
                      ),
                    ],
                  ),
              ],
            ),
          ),
          IconButton(
            tooltip: cantidadAvisos == 0
                ? 'Avisos: no hay avisos pendientes'
                : 'Avisos: $cantidadAvisos pendientes',
            iconSize: 28,
            onPressed: alTocarCampana,
            icon: Badge(
              isLabelVisible: cantidadAvisos > 0,
              label: Text('$cantidadAvisos'),
              backgroundColor: ColoresVet.rojo,
              child: const Icon(Icons.notifications_none,
                  color: ColoresVet.azulOscuro),
            ),
          ),
        ],
      ),
    );
  }
}

/// Título de sección, con una acción opcional a la derecha ("Ver todas").
class _Titulo extends StatelessWidget {
  const _Titulo(this.texto, {this.accion, this.alTocar, this.extra});

  final String texto;
  final String? accion;
  final VoidCallback? alTocar;
  final Widget? extra;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(20, 22, 8, 4),
      child: Row(
        children: [
          Expanded(
            child: Semantics(
              header: true,
              child: Text(texto,
                  style: Theme.of(context).textTheme.titleLarge?.copyWith(
                      color: ColoresVet.azulOscuro,
                      fontWeight: FontWeight.bold)),
            ),
          ),
          if (accion != null)
            TextButton.icon(
              onPressed: alTocar,
              iconAlignment: IconAlignment.end,
              icon: const Icon(Icons.arrow_forward, size: 18),
              label: Text(accion!,
                  style: const TextStyle(fontWeight: FontWeight.w600)),
            ),
          if (extra != null) extra!,
        ],
      ),
    );
  }
}

/// Aviso tocable (vacuna vencida, próxima dosis, todo al día).
class _TarjetaAviso extends StatelessWidget {
  const _TarjetaAviso({
    required this.tipo,
    required this.icono,
    required this.titulo,
    required this.detalle,
    this.alTocar,
  });

  final TipoAviso tipo;
  final IconData icono;
  final String titulo;
  final String detalle;
  final VoidCallback? alTocar;

  @override
  Widget build(BuildContext context) {
    final (fondo, borde, color) = switch (tipo) {
      TipoAviso.error => (
          ColoresVet.rojoFondo,
          ColoresVet.rojoBorde,
          ColoresVet.rojo
        ),
      TipoAviso.info => (
          ColoresVet.azulSuave,
          ColoresVet.azulBorde,
          ColoresVet.azulMedio
        ),
      TipoAviso.exito => (
          ColoresVet.moradoSuave,
          ColoresVet.moradoBorde,
          ColoresVet.moradoMedio
        ),
    };
    return Card(
      color: fondo,
      clipBehavior: Clip.antiAlias,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
        side: BorderSide(color: borde),
      ),
      child: ListTile(
        leading: Icon(icono, color: color, size: 28),
        title: Text(titulo,
            style: TextStyle(color: color, fontWeight: FontWeight.w600)),
        subtitle: Text(detalle, style: TextStyle(color: color)),
        trailing:
            alTocar == null ? null : Icon(Icons.chevron_right, color: color),
        onTap: alTocar == null
            ? null
            : () {
                // Si el aviso está en el panel de la campana, se cierra antes.
                Navigator.of(context)
                    .popUntil((r) => r is! ModalBottomSheetRoute);
                alTocar!();
              },
      ),
    );
  }
}

class _SinMascotas extends StatelessWidget {
  const _SinMascotas();

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.fromLTRB(16, 8, 16, 6),
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          children: [
            const Icon(Icons.pets, size: 56, color: ColoresVet.moradoMedio),
            const SizedBox(height: 12),
            Text('Aún no hay mascotas en su cuenta',
                textAlign: TextAlign.center,
                style: Theme.of(context).textTheme.titleMedium?.copyWith(
                    fontWeight: FontWeight.bold, color: ColoresVet.azulOscuro)),
            const SizedBox(height: 8),
            const Text(
              'La clínica registra a sus mascotas con su número de cédula. '
              'Cuando lo haga, aparecerán aquí.\n'
              'Deslice hacia abajo para actualizar.',
              textAlign: TextAlign.center,
              style: TextStyle(color: ColoresVet.textoSuave, height: 1.4),
            ),
          ],
        ),
      ),
    );
  }
}
