import { Routes } from '@angular/router';
import { roleGuard } from './core/guards/role.guard';
import { LoginComponent } from './features/login/login.component';

export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: 'login', component: LoginComponent },
  {
    path: 'veterinaria',
    canActivate: [roleGuard],
    data: { roles: ['Administrador', 'Veterinario'] },
    loadComponent: () =>
      import('./features/veterinaria/mascotas-listado/mascotas-listado.component').then(
        (m) => m.MascotasListadoComponent
      )
  },
  {
    path: 'veterinaria/mascotas/:id/expediente',
    canActivate: [roleGuard],
    data: { roles: ['Administrador', 'Veterinario'] },
    loadComponent: () =>
      import('./features/veterinaria/expediente-mascota/expediente-mascotas.component').then(
        (m) => m.ExpedienteMascotaComponent
      )
  },
  {
    // Módulo de administración: solo el rol Administrador (sección 3 del enunciado)
    path: 'admin',
    canActivate: [roleGuard],
    data: { roles: ['Administrador'] },
    loadComponent: () =>
      import('./features/admin/admin-layout/admin-layout.component').then((m) => m.AdminLayoutComponent),
    children: [
      { path: '', redirectTo: 'especies', pathMatch: 'full' },
      {
        path: 'especies',
        title: 'Especies y razas',
        loadComponent: () =>
          import('./features/admin/especies-razas/especies-razas.component').then(
            (m) => m.EspeciesRazasComponent
          )
      },
      {
        path: 'catalogo',
        title: 'Catálogo clínico',
        loadComponent: () =>
          import('./features/admin/catalogo-clinico/catalogo-clinico.component').then(
            (m) => m.CatalogoClinicoComponent
          )
      },
      {
        path: 'veterinarios',
        title: 'Veterinarios',
        loadComponent: () =>
          import('./features/admin/veterinarios/veterinarios.component').then((m) => m.VeterinariosComponent)
      },
      {
        path: 'usuarios',
        title: 'Usuarios',
        loadComponent: () =>
          import('./features/admin/usuarios/usuarios.component').then((m) => m.UsuariosComponent)
      }
    ]
  },
  { path: '**', redirectTo: 'login' }
];
