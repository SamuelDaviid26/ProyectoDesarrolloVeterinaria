import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  API_BASE_URL,
  ActualizarUsuarioRequest,
  CatalogoClinicoRequest,
  CatalogoClinicoResponse,
  CrearUsuarioRequest,
  EspecieRequest,
  EspecieResponse,
  RazaRequest,
  RazaResponse,
  TipoCatalogo,
  UsuarioResponse,
  VeterinarioResponse
} from '../models/api.models';

export interface VeterinarioFormPayload {
  nombreCompleto: string;
  numeroColegiado: string;
  foto?: File | null;
}

export interface FiltrosUsuarios {
  nombre?: string;
  cedula?: string;
  rol?: string;
  bloqueado?: boolean | null;
}

/**
 * Servicio con todas las llamadas a la API que usa el módulo de administración.
 * Las rutas de escritura solo las acepta el rol Administrador (ver controladores del BE).
 */
@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly baseUrl = API_BASE_URL;

  constructor(private readonly http: HttpClient) {}

  // ---------- Especies ----------

  listarEspecies(nombre?: string): Observable<EspecieResponse[]> {
    return this.http.get<EspecieResponse[]>(`${this.baseUrl}/api/especies`, {
      params: this.params({ nombre })
    });
  }

  crearEspecie(payload: EspecieRequest): Observable<EspecieResponse> {
    return this.http.post<EspecieResponse>(`${this.baseUrl}/api/especies`, payload);
  }

  actualizarEspecie(id: number, payload: EspecieRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/api/especies/${id}`, payload);
  }

  eliminarEspecie(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/api/especies/${id}`);
  }

  // ---------- Razas ----------

  listarRazas(especieId?: number | null, nombre?: string): Observable<RazaResponse[]> {
    return this.http.get<RazaResponse[]>(`${this.baseUrl}/api/razas`, {
      params: this.params({ especieId, nombre })
    });
  }

  crearRaza(payload: RazaRequest): Observable<RazaResponse> {
    return this.http.post<RazaResponse>(`${this.baseUrl}/api/razas`, payload);
  }

  actualizarRaza(id: number, payload: RazaRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/api/razas/${id}`, payload);
  }

  eliminarRaza(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/api/razas/${id}`);
  }

  // ---------- Catálogo clínico ----------

  listarCatalogo(nombre?: string, tipo?: TipoCatalogo | ''): Observable<CatalogoClinicoResponse[]> {
    return this.http.get<CatalogoClinicoResponse[]>(`${this.baseUrl}/api/catalogo-clinico`, {
      params: this.params({ nombre, tipo })
    });
  }

  crearElementoCatalogo(payload: CatalogoClinicoRequest): Observable<CatalogoClinicoResponse> {
    return this.http.post<CatalogoClinicoResponse>(`${this.baseUrl}/api/catalogo-clinico`, payload);
  }

  actualizarElementoCatalogo(id: number, payload: CatalogoClinicoRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/api/catalogo-clinico/${id}`, payload);
  }

  eliminarElementoCatalogo(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/api/catalogo-clinico/${id}`);
  }

  // ---------- Veterinarios (se envían como FormData por la foto) ----------

  listarVeterinarios(nombre?: string, numeroColegiado?: string): Observable<VeterinarioResponse[]> {
    return this.http.get<VeterinarioResponse[]>(`${this.baseUrl}/api/veterinarios`, {
      params: this.params({ nombre, numeroColegiado })
    });
  }

  crearVeterinario(payload: VeterinarioFormPayload): Observable<VeterinarioResponse> {
    return this.http.post<VeterinarioResponse>(
      `${this.baseUrl}/api/veterinarios`,
      this.formDataVeterinario(payload)
    );
  }

  actualizarVeterinario(id: number, payload: VeterinarioFormPayload): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/api/veterinarios/${id}`, this.formDataVeterinario(payload));
  }

  eliminarVeterinario(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/api/veterinarios/${id}`);
  }

  // ---------- Usuarios ----------

  listarUsuarios(filtros: FiltrosUsuarios = {}): Observable<UsuarioResponse[]> {
    return this.http.get<UsuarioResponse[]>(`${this.baseUrl}/api/usuarios`, {
      params: this.params({
        nombre: filtros.nombre,
        cedula: filtros.cedula,
        rol: filtros.rol,
        bloqueado: filtros.bloqueado
      })
    });
  }

  crearUsuario(payload: CrearUsuarioRequest): Observable<UsuarioResponse> {
    return this.http.post<UsuarioResponse>(`${this.baseUrl}/api/usuarios`, payload);
  }

  actualizarUsuario(id: string, payload: ActualizarUsuarioRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/api/usuarios/${id}`, payload);
  }

  cambiarBloqueo(id: string, bloqueado: boolean): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/api/usuarios/${id}/bloqueo`, { bloqueado });
  }

  eliminarUsuario(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/api/usuarios/${id}`);
  }

  // ---------- Utilidades ----------

  /** Arma los query params ignorando los valores vacíos. */
  private params(valores: Record<string, string | number | boolean | null | undefined>): HttpParams {
    let params = new HttpParams();
    for (const [clave, valor] of Object.entries(valores)) {
      if (valor === null || valor === undefined) continue;
      const texto = String(valor).trim();
      if (texto !== '') {
        params = params.set(clave, texto);
      }
    }
    return params;
  }

  private formDataVeterinario(payload: VeterinarioFormPayload): FormData {
    const formData = new FormData();
    formData.append('nombreCompleto', payload.nombreCompleto.trim());
    formData.append('numeroColegiado', payload.numeroColegiado.trim());
    if (payload.foto) {
      formData.append('foto', payload.foto, payload.foto.name);
    }
    return formData;
  }
}
