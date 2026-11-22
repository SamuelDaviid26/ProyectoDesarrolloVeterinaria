import { Inject, Injectable, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { API_BASE_URL, LoginRequest, LoginResponse } from '../models/api.models';

const STORAGE_KEY = 'vet_auth_session';

export interface AuthSession {
  token: string;
  expiraEnUtc: string;
  rol: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly isBrowser: boolean;

  constructor(
    private readonly http: HttpClient,
    private readonly router: Router,
    @Inject(PLATFORM_ID) platformId: object
  ) {
    this.isBrowser = isPlatformBrowser(platformId);
  }

  login(payload: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${API_BASE_URL}/api/auth/login`, payload).pipe(
      tap((res) => {
        this.guardarSesion({
          token: res.token,
          expiraEnUtc: res.expiraEnUtc,
          rol: res.rol
        });
      })
    );
  }

  logout(redireccionar = true): void {
    if (this.isBrowser) {
      localStorage.removeItem(STORAGE_KEY);
    }
    if (redireccionar) {
      void this.router.navigate(['/login']);
    }
  }

  obtenerSesion(): AuthSession | null {
    if (!this.isBrowser) {
      return null;
    }
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return null;
    }
    try {
      const parsed = JSON.parse(raw) as AuthSession;
      if (!parsed.token || !parsed.expiraEnUtc) {
        return null;
      }
      if (Date.parse(parsed.expiraEnUtc) <= Date.now()) {
        this.logout(false);
        return null;
      }
      return parsed;
    } catch {
      return null;
    }
  }

  obtenerToken(): string | null {
    return this.obtenerSesion()?.token ?? null;
  }

  obtenerRol(): string | null {
    return this.obtenerSesion()?.rol ?? null;
  }

  estaAutenticado(): boolean {
    return this.obtenerSesion() !== null;
  }

  tieneRolPermitido(rolesPermitidos: string[]): boolean {
    const rol = this.obtenerRol();
    return rol !== null && rolesPermitidos.includes(rol);
  }

  private guardarSesion(sesion: AuthSession): void {
    if (this.isBrowser) {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(sesion));
    }
  }
}