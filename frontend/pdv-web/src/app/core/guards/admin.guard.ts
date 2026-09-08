import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { TokenService } from '../auth/token.service';

export const adminGuard: CanActivateFn = () => {
  const tokens = inject(TokenService);
  const router = inject(Router);
  if (!tokens.has() || tokens.isExpired()) return router.createUrlTree(['/login']);
  return tokens.role()?.toLowerCase() === 'admin' ? true : router.createUrlTree(['/dashboard']);
};
