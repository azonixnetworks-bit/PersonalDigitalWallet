import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { TokenService } from '../auth/token.service';
import { ApiValidationProblem } from '../models/api-error.model';

export const apiErrorInterceptor: HttpInterceptorFn = (request, next) => {
  const tokens = inject(TokenService);
  const router = inject(Router);

  return next(request).pipe(catchError((error: unknown) => {
    if (!(error instanceof HttpErrorResponse)) {
      return throwError(() => new Error('Unable to connect to the server.'));
    }

    if (error.status === 401 && tokens.has()) {
      tokens.clear();
      void router.navigate(['/login'], { queryParams: { reason: 'unauthorized' } });
      return throwError(() => new Error('Your session is no longer valid. Please login again.'));
    }

    return throwError(() => new Error(extractMessage(error)));
  }));
};

function extractMessage(error: HttpErrorResponse): string {
  const body = error.error as ApiValidationProblem | string | null;
  if (typeof body === 'string' && body.trim()) return body.trim();
  if (body && typeof body === 'object') {
    if (typeof body.message === 'string' && body.message.trim()) return body.message.trim();
    const firstValidation = body.errors ? Object.values(body.errors).flat().find(Boolean) : undefined;
    if (firstValidation) return firstValidation;
    if (typeof body.detail === 'string' && body.detail.trim()) return body.detail.trim();
    if (typeof body.title === 'string' && body.title.trim()) return body.title.trim();
  }
  if (error.status === 0) return 'Unable to connect to the server.';
  if (error.status === 403) return 'You do not have permission to perform this action.';
  if (error.status === 404) return 'The requested resource was not found.';
  if (error.status === 429) return 'Too many requests. Please try again shortly.';
  return 'The request could not be completed.';
}
