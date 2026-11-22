import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const roleGuard: CanActivateFn = (route) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const rolesPermitidos = (route.data?.['roles'] as string[] | undefined) ?? [
    'Administrador',
    'Veterinario'
  ];

  if (!authService.estaAutenticado()) {
    return router.createUrlTree(['/login']);
  }

  if (!authService.tieneRolPermitido(rolesPermitidos)) {
    return router.createUrlTree(['/login'], {
      queryParams: { motivo: 'sin-permisos' }
    });
  }

  return true;
};