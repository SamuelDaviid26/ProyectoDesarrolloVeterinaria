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
  { path: '**', redirectTo: 'login' }
];