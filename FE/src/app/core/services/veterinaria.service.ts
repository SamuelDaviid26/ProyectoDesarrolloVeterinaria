import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  API_BASE_URL,
  CatalogoClinicoResponse,
  CrearNotaRequest,
  CrearTratamientoRequest,
  CrearVacunaRequest,
  DuenoResponse,
  EspecieResponse,
  ExamenResponse,
  MascotaResponse,
  MascotasListadoResponse,
  NotaResponse,
  RazaResponse,
  SuspenderTratamientoRequest,
  TratamientoResponse,
  TratamientosResponse,
  VacunaResponse
} from '../models/api.models';

export interface MascotaFormPayload {
  nombre: string;
  razaId: number;
  fechaNacimiento: string;
  sexo: string;
  duenoId: string;
  foto?: File | null;
}

@Injectable({ providedIn: 'root' })
export class VeterinariaService {
  private readonly baseUrl = API_BASE_URL;

  constructor(private readonly http: HttpClient) {}

  listarMascotas(filtros?: { nombre?: string; cedulaDueno?: string }): Observable<MascotasListadoResponse> {
    let params = new HttpParams();
    if (filtros?.nombre?.trim()) {
      params = params.set('nombre', filtros.nombre.trim());
    }
    if (filtros?.cedulaDueno?.trim()) {
      params = params.set('cedulaDueno', filtros.cedulaDueno.trim());
    }
    return this.http.get<MascotasListadoResponse>(`${this.baseUrl}/api/mascotas`, { params });
  }

  obtenerMascota(id: number): Observable<MascotaResponse> {
    return this.http.get<MascotaResponse>(`${this.baseUrl}/api/mascotas/${id}`);
  }

  crearMascota(payload: MascotaFormPayload): Observable<MascotaResponse> {
    return this.http.post<MascotaResponse>(`${this.baseUrl}/api/mascotas`, this.construirFormDataMascota(payload));
  }

  actualizarMascota(id: number, payload: MascotaFormPayload): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/api/mascotas/${id}`, this.construirFormDataMascota(payload));
  }

  obtenerArchivoBlob(urlRelativaOAbsoluta: string): Observable<Blob> {
    const url = urlRelativaOAbsoluta.startsWith('http')
      ? urlRelativaOAbsoluta
      : `${this.baseUrl}${urlRelativaOAbsoluta}`;
    return this.http.get(url, { responseType: 'blob' });
  }

  buscarDuenoPorCedula(cedula: string): Observable<DuenoResponse> {
    const params = new HttpParams().set('cedula', cedula.trim());
    return this.http.get<DuenoResponse>(`${this.baseUrl}/api/duenos/por-cedula`, { params });
  }

  crearDueno(payload: {
    cedula: string;
    nombreCompleto: string;
    correo: string;
    contrasena: string;
  }): Observable<DuenoResponse> {
    return this.http.post<DuenoResponse>(`${this.baseUrl}/api/duenos`, payload);
  }

  listarEspecies(): Observable<EspecieResponse[]> {
    return this.http.get<EspecieResponse[]>(`${this.baseUrl}/api/especies`);
  }

  listarRazas(especieId?: number): Observable<RazaResponse[]> {
    let params = new HttpParams();
    if (especieId) {
      params = params.set('especieId', especieId);
    }
    return this.http.get<RazaResponse[]>(`${this.baseUrl}/api/razas`, { params });
  }

  listarCatalogoClinico(tipo?: 'Medicamento' | 'Tratamiento', nombre?: string): Observable<CatalogoClinicoResponse[]> {
    let params = new HttpParams();
    if (tipo) {
      params = params.set('tipo', tipo);
    }
    if (nombre?.trim()) {
      params = params.set('nombre', nombre.trim());
    }
    return this.http.get<CatalogoClinicoResponse[]>(`${this.baseUrl}/api/catalogo-clinico`, { params });
  }

  listarVacunas(mascotaId: number): Observable<VacunaResponse[]> {
    return this.http.get<VacunaResponse[]>(`${this.baseUrl}/api/mascotas/${mascotaId}/vacunas`);
  }

  crearVacuna(mascotaId: number, payload: CrearVacunaRequest): Observable<VacunaResponse> {
    return this.http.post<VacunaResponse>(`${this.baseUrl}/api/mascotas/${mascotaId}/vacunas`, payload);
  }

  listarTratamientos(mascotaId: number): Observable<TratamientosResponse> {
    return this.http.get<TratamientosResponse>(`${this.baseUrl}/api/mascotas/${mascotaId}/tratamientos`);
  }

  crearTratamiento(mascotaId: number, payload: CrearTratamientoRequest): Observable<TratamientoResponse> {
    return this.http.post<TratamientoResponse>(`${this.baseUrl}/api/mascotas/${mascotaId}/tratamientos`, payload);
  }

  suspenderTratamiento(
    mascotaId: number,
    tratamientoId: number,
    payload: SuspenderTratamientoRequest = {}
  ): Observable<void> {
    return this.http.patch<void>(
      `${this.baseUrl}/api/mascotas/${mascotaId}/tratamientos/${tratamientoId}/suspension`,
      payload
    );
  }

  listarExamenes(mascotaId: number): Observable<ExamenResponse[]> {
    return this.http.get<ExamenResponse[]>(`${this.baseUrl}/api/mascotas/${mascotaId}/examenes`);
  }

  crearExamen(
    mascotaId: number,
    payload: { descripcion: string; archivo: File; veterinarioResponsableId?: number | null }
  ): Observable<ExamenResponse> {
    const formData = new FormData();
    formData.append('descripcion', payload.descripcion.trim());
    formData.append('archivo', payload.archivo, payload.archivo.name);
    if (payload.veterinarioResponsableId != null) {
      formData.append('veterinarioResponsableId', String(payload.veterinarioResponsableId));
    }
    return this.http.post<ExamenResponse>(`${this.baseUrl}/api/mascotas/${mascotaId}/examenes`, formData);
  }

  listarNotas(mascotaId: number): Observable<NotaResponse[]> {
    return this.http.get<NotaResponse[]>(`${this.baseUrl}/api/mascotas/${mascotaId}/notas`);
  }

  crearNota(mascotaId: number, payload: CrearNotaRequest): Observable<NotaResponse> {
    return this.http.post<NotaResponse>(`${this.baseUrl}/api/mascotas/${mascotaId}/notas`, payload);
  }

  private construirFormDataMascota(payload: MascotaFormPayload): FormData {
    const formData = new FormData();
    formData.append('nombre', payload.nombre.trim());
    formData.append('razaId', String(payload.razaId));
    formData.append('fechaNacimiento', payload.fechaNacimiento);
    formData.append('sexo', payload.sexo.trim());
    formData.append('duenoId', payload.duenoId.trim());
    if (payload.foto) {
      formData.append('foto', payload.foto, payload.foto.name);
    }
    return formData;
  }
}