import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from './auth.service';

export const roleGuard: CanActivateFn = route => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const roles = route.data?.['roles'] as string[] | undefined;

  if (!roles || roles.length === 0) {
    return true;
  }

  if (roles.includes(auth.getRole())) {
    return true;
  }

  return router.createUrlTree(['/login']);
};
