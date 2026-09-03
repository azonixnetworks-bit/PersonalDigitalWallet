import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { TokenService } from '../auth/token.service';

export const authGuard: CanActivateFn = () => {
  const tokens = inject(TokenService);
  const router = inject(Router);
  if (tokens.has() && !tokens.isExpired()) return true;
  const reason = tokens.has() ? 'expired' : 'unauthorized';
  tokens.clear();
  return router.createUrlTree(['/login'], { queryParams: { reason } });
};
