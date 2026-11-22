import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

/**
 * Contenedor del módulo de administración: encabezado, pestañas y <router-outlet>
 * donde se cargan las pantallas hijas (especies, catálogo, veterinarios, usuarios).
 */
@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './admin-layout.component.html'
})
export class AdminLayoutComponent {
  readonly secciones = [
    { ruta: 'especies', titulo: 'Especies y razas' },
    { ruta: 'catalogo', titulo: 'Catálogo clínico' },
    { ruta: 'veterinarios', titulo: 'Veterinarios' },
    { ruta: 'usuarios', titulo: 'Usuarios' }
  ];

  constructor(private readonly authService: AuthService) {}

  cerrarSesion(): void {
    this.authService.logout(true);
  }
}
