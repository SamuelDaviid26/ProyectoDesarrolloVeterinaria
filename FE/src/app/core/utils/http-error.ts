import { HttpErrorResponse } from '@angular/common/http';

/**
 * ASP.NET Identity devuelve los errores de contraseña en inglés.
 * Este diccionario traduce los más comunes para mostrarlos al usuario.
 */
const TRADUCCIONES_IDENTITY: [RegExp, string][] = [
  [/at least (\d+) characters/i, 'La contraseña debe tener al menos $1 caracteres.'],
  [/non alphanumeric/i, 'La contraseña debe incluir al menos un símbolo (por ejemplo ! @ # .).'],
  [/at least one digit/i, 'La contraseña debe incluir al menos un número.'],
  [/at least one lowercase/i, 'La contraseña debe incluir al menos una letra minúscula.'],
  [/at least one uppercase/i, 'La contraseña debe incluir al menos una letra mayúscula.'],
  [/is already taken/i, 'El correo ya está registrado.'],
  [/is invalid/i, 'El correo no es válido.']
];

function traducir(mensaje: string): string {
  for (const [patron, traduccion] of TRADUCCIONES_IDENTITY) {
    const coincidencia = mensaje.match(patron);
    if (coincidencia) {
      return traduccion.replace('$1', coincidencia[1] ?? '');
    }
  }
  return mensaje;
}

/**
 * Convierte cualquier error de la API en un mensaje legible en español.
 * La API responde de tres formas: { mensaje }, { errores: [] } o ProblemDetails con { errors: {} }.
 */
export function mensajeDeError(err: unknown, porDefecto: string): string {
  if (!(err instanceof HttpErrorResponse)) {
    return porDefecto;
  }

  if (err.status === 0) {
    return 'No se pudo conectar con el servidor de la API.';
  }

  const cuerpo = err.error;

  if (typeof cuerpo?.mensaje === 'string' && cuerpo.mensaje) {
    return cuerpo.mensaje;
  }

  if (Array.isArray(cuerpo?.errores) && cuerpo.errores.length > 0) {
    return cuerpo.errores.map((e: string) => traducir(e)).join(' ');
  }

  if (cuerpo?.errors && typeof cuerpo.errors === 'object') {
    const mensajes = Object.values(cuerpo.errors as Record<string, string[]>).flat();
    if (mensajes.length > 0) {
      return mensajes.map((e) => traducir(e)).join(' ');
    }
  }

  if (err.status === 403) {
    return 'No tenés permisos para realizar esta acción.';
  }
  if (err.status === 404) {
    return 'El registro ya no existe. Recargá la lista.';
  }

  return porDefecto;
}
