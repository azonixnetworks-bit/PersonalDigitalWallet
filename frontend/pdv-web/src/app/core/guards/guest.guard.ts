import { inject } from '@angular/core';
import { CanActivateChildFn, Router } from '@angular/router';
import { TokenService } from '../auth/token.service';

export const guestGuard: CanActivateChildFn = () => {
  const tokens = inject(TokenService);
  const router = inject(Router);
  return tokens.has() && !tokens.isExpired() ? router.createUrlTree(['/dashboard']) : true;
};
