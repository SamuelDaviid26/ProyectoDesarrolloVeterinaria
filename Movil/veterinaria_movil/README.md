# Veterinaria — aplicación móvil (Flutter, Android)

Aplicación de consulta para el perfil **Dueño**. Inicia sesión con JWT y muestra
sus mascotas, las vacunas (marcando las vencidas y las próximas dosis), los
resultados de exámenes (ver dentro de la app o descargar), los medicamentos y
tratamientos (vigentes y suspendidos) y el historial clínico.

Usa los mismos colores que la aplicación web (azules, morados y blanco; ver
`lib/tema.dart`).

## Pantallas

- **Bienvenida**: presentación de la app con "Iniciar sesión" y "Crear cuenta".
- **Iniciar sesión / Crear cuenta**: el dueño se registra con su cédula; con esa
  cédula la clínica le asigna las mascotas.
- **Barra inferior** con tres pestañas:
  - **Inicio**: saludo, campana de avisos (vacunas vencidas y próximas dosis),
    buscador, filtro por especie, tarjetas de las mascotas y tratamientos vigentes.
  - **Mis mascotas**: todas las mascotas en tarjetas, con buscador y filtro.
  - **Mi cuenta**: datos de la sesión, resumen de las mascotas, ayuda y cerrar sesión.
- **Expediente de la mascota**: vacunas, exámenes, tratamientos e historial.
- **Visor de exámenes**: PDF o imagen dentro de la app, con zoom.

## Estructura

```
lib/
├── main.dart                   arranque, tema y regreso al login si la sesión expira
├── config.dart                 dirección de la API (API_URL)
├── tema.dart                   colores de la web y tema de la app
├── models/modelos.dart         Mascota, Vacuna, Examen, Tratamiento, Nota (DTOs de la API)
│                               y ResumenMascota (lo que muestra el Inicio)
├── services/
│   ├── api_client.dart         única clase que llama a la API: token, errores de red y 401
│   ├── sesion.dart             token JWT y correo en almacenamiento seguro
│   ├── auth_service.dart       login y registro (api/auth)
│   └── mascotas_service.dart   consultas de api/mis-mascotas, ver y guardar exámenes
├── widgets/
│   ├── comunes.dart            VistaCarga (cargando / error / reintentar), avisos, encabezados
│   └── tarjeta_mascota.dart    tarjeta grande de mascota, buscador y filtro por especie
└── screens/                    bienvenida, login, registro, principal (barra inferior),
                                inicio, mis mascotas, cuenta, expediente, vacunas,
                                exámenes, visor de examen, tratamientos, historial
```

## Endpoints que usa

| Pantalla | Endpoint |
|---|---|
| Iniciar sesión | `POST /api/auth/login` |
| Crear cuenta | `POST /api/auth/registro` |
| Mascotas | `GET /api/mis-mascotas` y `GET /api/mis-mascotas/{id}/foto` |
| Vacunas | `GET /api/mis-mascotas/{id}/vacunas` |
| Exámenes | `GET /api/mis-mascotas/{id}/examenes` y `.../examenes/{examenId}/archivo` |
| Tratamientos | `GET /api/mis-mascotas/{id}/tratamientos` |
| Historial | `GET /api/mis-mascotas/{id}/notas` |

El Inicio y Mi cuenta juntan las vacunas y los tratamientos de todas las
mascotas en la app; no usan endpoints adicionales.

## Ejecutar

La dirección de la API se pasa al compilar:

```
flutter pub get
flutter run --dart-define=API_URL=https://veterinaria-arcejosa.somee.com
```

- API local con celular físico: la API debe escuchar en la red
  (`dotnet run --urls http://0.0.0.0:5051`) y el celular estar en la misma red Wi-Fi,
  o conectado por USB con `adb reverse tcp:5051 tcp:5051`.
- Emulador: `http://10.0.2.2:5051` (valor por defecto).

## Generar el APK

```
flutter build apk --release --dart-define=API_URL=https://veterinaria-arcejosa.somee.com
```

El archivo queda en `build/app/outputs/flutter-apk/app-release.apk`.

## Pruebas

```
flutter test
```

## Errores de red y sesión

Todas las peticiones pasan por `ApiClient`: sin conexión, tiempo de espera o
error del servidor se muestran como mensaje con botón **Reintentar** (la app no
se cierra). Si el token vence o la API responde 401, se cierra la sesión y se
vuelve al login con el aviso "Su sesión expiró".

## Pendiente del backend

El login devuelve solo `token`, `expiraEnUtc` y `rol`, por eso la app identifica
la sesión por el correo. Si el backend agrega `nombreCompleto` y `cedula` a
`LoginResponse`, la app ya los muestra ("Hola, Charle" y en Mi cuenta).
