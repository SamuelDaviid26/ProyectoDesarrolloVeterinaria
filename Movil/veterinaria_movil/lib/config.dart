/// Dirección de la API REST.
///
/// Se cambia al compilar, sin tocar el código:
///   flutter run --dart-define=API_URL=http://192.168.1.50:5051
///   flutter build apk --dart-define=API_URL=https://servidor-publico
///
/// El valor por defecto (10.0.2.2) es la computadora vista desde el emulador.
const String apiUrl = String.fromEnvironment(
  'API_URL',
  defaultValue: 'http://10.0.2.2:5051',
);
