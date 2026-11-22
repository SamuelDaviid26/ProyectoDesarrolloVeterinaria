import 'package:flutter/material.dart';

import '../config.dart';
import '../services/api_client.dart';
import '../tema.dart';

/// dd/mm/aaaa
String formatoFecha(DateTime fecha) =>
    '${fecha.day.toString().padLeft(2, '0')}/'
    '${fecha.month.toString().padLeft(2, '0')}/${fecha.year}';

/// dd/mm/aaaa hh:mm
String formatoFechaHora(DateTime fecha) => '${formatoFecha(fecha)} '
    '${fecha.hour.toString().padLeft(2, '0')}:${fecha.minute.toString().padLeft(2, '0')}';

/// Centra el contenido y limita su ancho, para que en tablets
/// no se estire de borde a borde.
class AnchoMaximo extends StatelessWidget {
  const AnchoMaximo({super.key, this.ancho = 760, required this.child});

  final double ancho;
  final Widget child;

  @override
  Widget build(BuildContext context) => Center(
        child: ConstrainedBox(
          constraints: BoxConstraints(maxWidth: ancho),
          child: child,
        ),
      );
}

/// Carga datos de la API y muestra, según el caso:
/// cargando -> indicador; error -> mensaje claro con "Reintentar";
/// listo -> el contenido, que se puede refrescar deslizando hacia abajo.
class VistaCarga<T> extends StatefulWidget {
  const VistaCarga({
    super.key,
    required this.cargar,
    required this.construir,
    this.mensajeCarga = 'Cargando…',
  });

  final Future<T> Function() cargar;

  /// Texto bajo el indicador mientras se espera la respuesta de la API.
  final String mensajeCarga;

  /// Debe devolver un widget desplazable (ListView), para que funcione
  /// el "deslizar para refrescar".
  final Widget Function(BuildContext context, T datos) construir;

  @override
  State<VistaCarga<T>> createState() => VistaCargaState<T>();
}

/// Pública para que una pantalla pueda pedir "Actualizar" con un GlobalKey.
class VistaCargaState<T> extends State<VistaCarga<T>> {
  /// Vuelve a pedir los datos a la API (botón "Actualizar").
  Future<void> recargar() => _recargar();

  late Future<T> _futuro = widget.cargar();

  Future<void> _recargar() async {
    final futuro = widget.cargar();
    // Con llaves: setState no acepta una función que devuelva un Future.
    setState(() {
      _futuro = futuro;
    });
    try {
      await futuro;
    } catch (_) {
      // El error lo muestra el FutureBuilder.
    }
  }

  @override
  Widget build(BuildContext context) {
    return AnchoMaximo(child: _contenido());
  }

  Widget _contenido() {
    return FutureBuilder<T>(
      future: _futuro,
      builder: (context, estado) {
        if (estado.connectionState != ConnectionState.done) {
          return Center(
            child: Semantics(
              liveRegion: true,
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  const CircularProgressIndicator(),
                  const SizedBox(height: 16),
                  Text(widget.mensajeCarga,
                      style: TextStyle(
                          color:
                              Theme.of(context).colorScheme.onSurfaceVariant)),
                ],
              ),
            ),
          );
        }
        if (estado.hasError) {
          final error = estado.error;
          return MensajeEstado(
            icono: Icons.wifi_off_rounded,
            titulo: 'No se pudo cargar',
            mensaje: error is ApiException
                ? error.mensaje
                : 'Ocurrió un error inesperado. Intente de nuevo.',
            alReintentar: error is ApiException && error.sesionExpirada
                ? null
                : _recargar,
          );
        }
        return RefreshIndicator(
          onRefresh: _recargar,
          child: widget.construir(context, estado.data as T),
        );
      },
    );
  }
}

/// Pantalla de estado: error, lista vacía, etc.
class MensajeEstado extends StatelessWidget {
  const MensajeEstado({
    super.key,
    required this.icono,
    required this.titulo,
    required this.mensaje,
    this.alReintentar,
  });

  final IconData icono;
  final String titulo;
  final String mensaje;
  final VoidCallback? alReintentar;

  @override
  Widget build(BuildContext context) {
    final colores = Theme.of(context).colorScheme;
    // ListView para que también se pueda deslizar y refrescar.
    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.fromLTRB(32, 96, 32, 32),
      children: [
        Icon(icono, size: 64, color: colores.outline),
        const SizedBox(height: 16),
        Text(titulo,
            textAlign: TextAlign.center,
            style: Theme.of(context).textTheme.titleLarge),
        const SizedBox(height: 8),
        Text(mensaje,
            textAlign: TextAlign.center,
            style: TextStyle(color: colores.onSurfaceVariant)),
        if (alReintentar != null) ...[
          const SizedBox(height: 24),
          Center(
            child: FilledButton.icon(
              onPressed: alReintentar,
              icon: const Icon(Icons.refresh),
              label: const Text('Reintentar'),
            ),
          ),
        ],
      ],
    );
  }
}

/// Foto de la mascota. La pide a la API con el token; si no tiene foto
/// o no carga, muestra una huella.
class FotoMascota extends StatelessWidget {
  const FotoMascota({super.key, required this.fotoUrl, this.tamano = 56});

  final String? fotoUrl;
  final double tamano;

  @override
  Widget build(BuildContext context) {
    final colores = Theme.of(context).colorScheme;
    final sinFoto = Container(
      width: tamano,
      height: tamano,
      color: colores.primaryContainer,
      child: Icon(Icons.pets,
          size: tamano * 0.5, color: colores.onPrimaryContainer),
    );

    return ClipRRect(
      borderRadius: BorderRadius.circular(tamano / 4),
      child: fotoUrl == null
          ? sinFoto
          : Image.network(
              '$apiUrl$fotoUrl',
              headers: ApiClient.instancia.encabezados(),
              width: tamano,
              height: tamano,
              fit: BoxFit.cover,
              errorBuilder: (_, __, ___) => sinFoto,
            ),
    );
  }
}

/// Etiqueta de color (Vencida, Pendiente, Medicamento...).
class Etiqueta extends StatelessWidget {
  const Etiqueta(
      {super.key,
      required this.texto,
      required this.fondo,
      required this.color});

  final String texto;
  final Color fondo;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration:
          BoxDecoration(color: fondo, borderRadius: BorderRadius.circular(20)),
      child: Text(texto,
          style: TextStyle(
              color: color, fontWeight: FontWeight.w600, fontSize: 12)),
    );
  }
}

/// Logo de la clínica (el mismo de la web) dentro de un círculo morado,
/// como .vet-logo-ring de la web.
class LogoVet extends StatelessWidget {
  const LogoVet({super.key, this.tamano = 72});

  final double tamano;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: tamano,
      height: tamano,
      padding: EdgeInsets.all(tamano * 0.16),
      decoration: BoxDecoration(
        color: ColoresVet.moradoMedio,
        shape: BoxShape.circle,
        border: Border.all(
            color: const Color(0x73DDD6FE), width: 2), // borde lila suave
      ),
      child: Image.asset('assets/logo.png'),
    );
  }
}

/// Encabezado con el degradado azul → morado de la web (.vet-card-header).
class EncabezadoVet extends StatelessWidget {
  const EncabezadoVet({
    super.key,
    required this.titulo,
    required this.subtitulo,
    this.imagen,
  });

  final String titulo;
  final String subtitulo;

  /// Lo que va arriba del título; si no se da, el logo.
  final Widget? imagen;

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: const BoxDecoration(gradient: ColoresVet.degradado),
      padding: const EdgeInsets.fromLTRB(24, 28, 24, 24),
      child: Column(
        children: [
          imagen ?? const LogoVet(),
          const SizedBox(height: 14),
          Text(titulo,
              textAlign: TextAlign.center,
              style: Theme.of(context)
                  .textTheme
                  .headlineSmall
                  ?.copyWith(color: Colors.white, fontWeight: FontWeight.bold)),
          const SizedBox(height: 4),
          Text(subtitulo,
              textAlign: TextAlign.center,
              style: const TextStyle(color: Color(0xEBEDE9FE))),
        ],
      ),
    );
  }
}

enum TipoAviso { error, info, exito }

/// Recuadro de mensaje con ícono, igual a los avisos de la web:
/// rojo = error, ámbar = información, verde = éxito.
/// No depende solo del color: cada tipo tiene su propio ícono.
class Aviso extends StatelessWidget {
  const Aviso({super.key, required this.texto, this.tipo = TipoAviso.error});

  final String texto;
  final TipoAviso tipo;

  @override
  Widget build(BuildContext context) {
    final (fondo, borde, color, icono) = switch (tipo) {
      TipoAviso.error => (
          ColoresVet.rojoFondo,
          ColoresVet.rojoBorde,
          ColoresVet.rojo,
          Icons.error_outline
        ),
      TipoAviso.info => (
          ColoresVet.azulSuave,
          ColoresVet.azulBorde,
          ColoresVet.azulMedio,
          Icons.info_outline
        ),
      TipoAviso.exito => (
          ColoresVet.moradoSuave,
          ColoresVet.moradoBorde,
          ColoresVet.moradoMedio,
          Icons.check_circle_outline
        ),
    };
    return Semantics(
      liveRegion: true,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
        decoration: BoxDecoration(
          color: fondo,
          border: Border.all(color: borde),
          borderRadius: BorderRadius.circular(12),
        ),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            ExcludeSemantics(child: Icon(icono, color: color, size: 22)),
            const SizedBox(width: 10),
            Expanded(
              child: Text(texto,
                  style: TextStyle(color: color, fontSize: 14, height: 1.4)),
            ),
          ],
        ),
      ),
    );
  }
}

/// Validación básica de correo (algo@dominio.ext), igual de estricta
/// que la API, para que el error salga en español antes de enviar.
bool esCorreoValido(String correo) =>
    RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$').hasMatch(correo);

/// Encabezado claro de las pestañas (sin barra de color): flecha para
/// volver (opcional), título grande y una acción a la derecha (opcional).
class EncabezadoPestana extends StatelessWidget {
  const EncabezadoPestana({
    super.key,
    required this.titulo,
    this.subtitulo,
    this.alVolver,
    this.accion,
  });

  final String titulo;
  final String? subtitulo;
  final VoidCallback? alVolver;
  final Widget? accion;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.fromLTRB(alVolver == null ? 20 : 4, 12, 12, 8),
      child: Row(
        children: [
          if (alVolver != null)
            IconButton(
              tooltip: 'Volver al inicio',
              onPressed: alVolver,
              icon: const Icon(Icons.arrow_back, color: ColoresVet.azulOscuro),
            ),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Semantics(
                  header: true,
                  child: Text(titulo,
                      style: Theme.of(context)
                          .textTheme
                          .headlineMedium
                          ?.copyWith(
                              color: ColoresVet.azulOscuro,
                              fontWeight: FontWeight.bold)),
                ),
                if (subtitulo != null)
                  Text(subtitulo!,
                      style: const TextStyle(color: ColoresVet.textoSuave)),
              ],
            ),
          ),
          if (accion != null) accion!,
        ],
      ),
    );
  }
}

/// Botón "Actualizar" (ícono + texto), como en las apps móviles.
class BotonActualizar extends StatelessWidget {
  const BotonActualizar({super.key, required this.alTocar});

  final VoidCallback alTocar;

  @override
  Widget build(BuildContext context) => TextButton.icon(
        onPressed: alTocar,
        icon: const Icon(Icons.refresh, size: 20),
        label: const Text('Actualizar',
            style: TextStyle(fontWeight: FontWeight.w600)),
      );
}
