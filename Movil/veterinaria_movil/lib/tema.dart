import 'package:flutter/material.dart';

/// Colores de la aplicación web (FE/src/app/.../*.css, variables --vet-*),
/// para que la app móvil y la web se vean como un mismo sistema.
/// La app usa solo los azules, morados y blanco; el rojo queda para avisar
/// de lo vencido o de un error.
class ColoresVet {
  ColoresVet._();

  static const fondo = Color(0xFFF5F7FC); // --vet-bg
  static const azulOscuro = Color(0xFF162D5A); // --vet-blue-dark
  static const azulMedio = Color(0xFF284B8A); // --vet-blue-mid
  static const moradoMedio = Color(0xFF5B3B8C); // --vet-purple-mid
  static const moradoBrillante = Color(0xFF6D46A8); // --vet-purple-bright
  static const moradoSuave = Color(0xFFEDE9FE); // --vet-purple-soft

  // Mensajes de error de la web (Tailwind red-50 / red-200 / red-700).
  static const rojoFondo = Color(0xFFFEF2F2);
  static const rojoBorde = Color(0xFFFECACA);
  static const rojo = Color(0xFFB91C1C);

  // Avisos informativos (azul) y de éxito (morado).
  static const azulSuave = Color(0xFFE6EDF8);
  static const azulBorde = Color(0xFFBFD0EE);
  static const moradoBorde = Color(0xFFDDD6FE);

  static const textoOscuro = Color(0xFF0F172A); // slate-900
  static const textoSuave = Color(0xFF64748B); // slate-500
  static const borde = Color(0xFFCBD5E1); // slate-300
  static const bordeTarjeta = Color(0x24162D5A); // azul oscuro al 14 %

  /// Degradado de los encabezados de la web (.vet-card-header).
  static const degradado = LinearGradient(
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
    colors: [azulOscuro, moradoBrillante],
  );
}

ThemeData temaVeterinaria() {
  const esquema = ColorScheme(
    brightness: Brightness.light,
    primary: ColoresVet.azulOscuro,
    onPrimary: Colors.white,
    primaryContainer: ColoresVet.moradoSuave,
    onPrimaryContainer: ColoresVet.moradoMedio,
    secondary: ColoresVet.moradoMedio,
    onSecondary: Colors.white,
    secondaryContainer: ColoresVet.moradoSuave,
    onSecondaryContainer: ColoresVet.moradoMedio,
    tertiary: ColoresVet.moradoBrillante,
    onTertiary: Colors.white,
    error: ColoresVet.rojo,
    onError: Colors.white,
    errorContainer: ColoresVet.rojoFondo,
    onErrorContainer: ColoresVet.rojo,
    surface: Colors.white,
    onSurface: ColoresVet.textoOscuro,
    onSurfaceVariant: ColoresVet.textoSuave,
    outline: ColoresVet.borde,
    outlineVariant: Color(0xFFE2E8F0),
  );

  final redondeo = BorderRadius.circular(12);

  return ThemeData(
    colorScheme: esquema,
    useMaterial3: true,
    scaffoldBackgroundColor: ColoresVet.fondo,
    appBarTheme: const AppBarTheme(
      backgroundColor: ColoresVet.azulOscuro,
      foregroundColor: Colors.white,
      elevation: 0,
      // Línea morada bajo el encabezado.
      shape: Border(
          bottom: BorderSide(color: ColoresVet.moradoBrillante, width: 3)),
    ),
    cardTheme: CardThemeData(
      color: Colors.white,
      elevation: 0,
      margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 6),
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
        side: const BorderSide(color: ColoresVet.bordeTarjeta),
      ),
    ),
    // Botón principal azul oscuro, como .vet-btn-blue de la web.
    filledButtonTheme: FilledButtonThemeData(
      style: FilledButton.styleFrom(
        backgroundColor: ColoresVet.azulOscuro,
        foregroundColor: Colors.white,
        minimumSize: const Size(64, 52),
        textStyle: const TextStyle(fontWeight: FontWeight.w600, fontSize: 16),
        shape: RoundedRectangleBorder(borderRadius: redondeo),
      ),
    ),
    textButtonTheme: TextButtonThemeData(
      style: TextButton.styleFrom(foregroundColor: ColoresVet.moradoMedio),
    ),
    inputDecorationTheme: InputDecorationTheme(
      filled: true,
      fillColor: const Color(0xFFF8FAFC),
      border: OutlineInputBorder(borderRadius: redondeo),
      enabledBorder: OutlineInputBorder(
        borderRadius: redondeo,
        borderSide: const BorderSide(color: ColoresVet.borde),
      ),
      // Borde morado al escribir, como .vet-input:focus de la web.
      focusedBorder: OutlineInputBorder(
        borderRadius: redondeo,
        borderSide:
            const BorderSide(color: ColoresVet.moradoBrillante, width: 2),
      ),
      floatingLabelStyle: const TextStyle(color: ColoresVet.moradoBrillante),
    ),
    progressIndicatorTheme:
        const ProgressIndicatorThemeData(color: ColoresVet.moradoBrillante),
    dialogTheme: DialogThemeData(
      backgroundColor: Colors.white,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
    ),
  );
}
