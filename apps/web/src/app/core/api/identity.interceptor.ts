import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';

import { Identity } from '../identity/identity';

export const identityInterceptor: HttpInterceptorFn = (request, next) => {
  const user = inject(Identity).user();
  if (!user || !request.url.startsWith('/api/')) return next(request);
  const header = user.role === 'claimant' ? 'X-Claimant-Email' : 'X-Staff-Id';
  return next(request.clone({ setHeaders: { [header]: user.identity } }));
};
