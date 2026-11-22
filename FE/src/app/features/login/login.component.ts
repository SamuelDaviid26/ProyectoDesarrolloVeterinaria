import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent implements OnInit {
  correo = '';
  contrasena = '';
  cargando = false;
  mensajeError = '';
  avisoPermisos = false;

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router,
    private readonly route: ActivatedRoute,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.avisoPermisos = this.route.snapshot.queryParamMap.get('motivo') === 'sin-permisos';
    if (this.authService.estaAutenticado()) {
      this.irAlModulo(this.authService.obtenerRol());
    }
  }

  onSubmit(): void {
    this.mensajeError = '';
    this.avisoPermisos = false;

    if (!this.correo.trim() || !this.contrasena) {
      this.mensajeError = 'Completá tu correo electrónico y contraseña.';
      this.cdr.detectChanges();
      return;
    }

    this.cargando = true;
    this.authService.login({ correo: this.correo.trim(), contrasena: this.contrasena }).subscribe({
      next: (res) => {
        this.cargando = false;
        if (res.rol === 'Veterinario' || res.rol === 'Administrador') {
          this.irAlModulo(res.rol);
        } else {
          this.authService.logout(false);
          this.mensajeError = 'El perfil de Dueño solo está disponible en la aplicación móvil.';
          this.cdr.detectChanges();
        }
      },
      error: (err: HttpErrorResponse) => {
        this.cargando = false;
        this.mensajeError =
          err.error?.mensaje ||
          (err.status === 401
            ? 'Correo o contraseña incorrectos, o cuenta bloqueada.'
            : 'No se pudo conectar con el servidor de la API.');
            this.cdr.detectChanges();
      }
    });
  }

  /** El administrador entra al módulo de administración; el veterinario, al de veterinaria. */
  private irAlModulo(rol: string | null): void {
    if (rol === 'Administrador') {
      void this.router.navigate(['/admin']);
    } else if (rol === 'Veterinario') {
      void this.router.navigate(['/veterinaria']);
    }
  }
}
