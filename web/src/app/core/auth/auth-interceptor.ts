import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { API_BASE } from '../api/api-config';
import { AuthStore } from './auth-store';

/**
 * Attaches the bearer token, but only to our own API.
 *
 * An interceptor that attaches unconditionally will happily send the token to any host
 * the app ever calls — an analytics endpoint, a CDN, a third-party widget — handing the
 * session to someone else. The origin check is the whole point.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthStore);

  if (!request.url.startsWith(API_BASE)) {
    return next(request);
  }

  const token = auth.token();
  const outbound = token
    ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : request;

  return next(outbound).pipe(
    catchError((error: HttpErrorResponse) => {
      // The token was rejected mid-session, so drop it rather than retrying with
      // credentials the server has already refused.
      if (error.status === 401 && token) {
        auth.logout();
      }

      return throwError(() => error);
    }),
  );
};
