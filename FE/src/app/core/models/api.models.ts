export const API_BASE_URL = 'http://localhost:5051';

export type RolUsuario = 'Administrador' | 'Veterinario' | 'Dueño';

export interface LoginRequest {
  correo: string;
  contrasena: string;
}

export interface LoginResponse {
  token: string;
  expiraEnUtc: string;
  rol: RolUsuario | string;
}

export interface DuenoResponse {
  id: string;
  cedula: string;
  nombreCompleto: string;
}

export interface RegistroDuenoRequest {
  cedula: string;
  nombreCompleto: string;
  correo: string;
  contrasena: string;
}

export interface EspecieResponse {
  id: number;
  nombre: string;
}

export interface RazaResponse {
  id: number;
  nombre: string;
  especieId: number;
  especieNombre: string;
}

export interface CatalogoClinicoResponse {
  id: number;
  nombre: string;
  tipo: 'Medicamento' | 'Tratamiento' | string;
  descripcion: string;
}

export interface MascotaListadoResponse {
  id: number;
  nombre: string;
  duenoId: string;
  duenoNombreCompleto: string;
  duenoCedula: string;
  especieId: number;
  especieNombre: string;
  razaId: number;
  razaNombre: string;
  fechaNacimiento: string;
  fotoUrl: string | null;
  ultimaAtencion: string | null;
}

export interface MascotasListadoResponse {
  atencionesRecientes: MascotaListadoResponse[];
  sinAtenciones: MascotaListadoResponse[];
}

export interface MascotaResponse {
  id: number;
  nombre: string;
  razaId: number;
  razaNombre: string;
  especieId: number;
  especieNombre: string;
  fechaNacimiento: string;
  sexo: string;
  duenoId: string;
  duenoNombreCompleto: string;
  duenoCedula: string;
  fotoUrl: string | null;
}

export interface VacunaResponse {
  id: number;
  nombreVacuna: string;
  fechaAplicacion: string;
  fechaProximaDosis: string | null;
  registradoEn: string;
  vacunaAnteriorId: number | null;
  veterinarioResponsableId: number;
  veterinarioResponsableNombre: string;
  registradoPorId: string;
  registradoPorNombre: string;
  estadoProximaDosis: 'Cumplida' | 'Vencida' | 'Pendiente' | null | string;
}

export interface CrearVacunaRequest {
  nombreVacuna: string;
  fechaAplicacion: string;
  fechaProximaDosis?: string | null;
  vacunaAnteriorId?: number | null;
  veterinarioResponsableId?: number | null;
}

export interface TratamientoResponse {
  id: number;
  elementoCatalogoClinicoId: number;
  nombre: string;
  descripcion: string;
  tipo: 'Medicamento' | 'Tratamiento' | string;
  asignadoEn: string;
  veterinarioResponsableId: number;
  veterinarioResponsableNombre: string;
  registradoPorId: string;
  registradoPorNombre: string;
  suspendidoEn: string | null;
  veterinarioSuspensionResponsableId: number | null;
  veterinarioSuspensionResponsableNombre: string | null;
  suspendidoPorId: string | null;
  suspendidoPorNombre: string | null;
}

export interface TratamientosResponse {
  vigentes: TratamientoResponse[];
  suspendidos: TratamientoResponse[];
}

export interface CrearTratamientoRequest {
  elementoCatalogoClinicoId: number;
  veterinarioResponsableId?: number | null;
}

export interface SuspenderTratamientoRequest {
  veterinarioResponsableId?: number | null;
}

export interface ExamenResponse {
  id: number;
  descripcion: string;
  registradoEn: string;
  archivoId: number;
  archivoNombre: string;
  archivoTipoContenido: string;
  archivoUrl: string;
  veterinarioResponsableId: number;
  veterinarioResponsableNombre: string;
  registradoPorId: string;
  registradoPorNombre: string;
}

export interface NotaResponse {
  id: number;
  contenidoHtml: string;
  registradoEn: string;
  veterinarioResponsableId: number;
  veterinarioResponsableNombre: string;
  registradoPorId: string;
  registradoPorNombre: string;
}

export interface CrearNotaRequest {
  contenidoHtml: string;
  veterinarioResponsableId?: number | null;
}