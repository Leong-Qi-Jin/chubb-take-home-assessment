import { inject } from '@angular/core';
import { CanActivateFn, Router, Routes } from '@angular/router';

import { Identity, ROLE_ROUTES, UserRole } from './core/identity/identity';
import { AppShell } from './layout/app-shell';

const roleGuard =
  (role: UserRole): CanActivateFn =>
  () => {
    const identity = inject(Identity);
    return (
      identity.role() === role || inject(Router).createUrlTree(['/not-allowed'])
    );
  };

export const routes: Routes = [
  {
    path: '',
    component: AppShell,
    children: [
      {
        // Each role lands on its own home page.
        path: '',
        pathMatch: 'full',
        redirectTo: () =>
          ROLE_ROUTES[inject(Identity).role() ?? 'claimant'].home,
      },
      {
        path: 'dashboard',
        title: 'Dashboard',
        canActivate: [roleGuard('officer')],
        loadComponent: () =>
          import('./features/dashboard/dashboard').then(
            (module) => module.Dashboard,
          ),
      },
      {
        path: 'queue',
        canActivate: [roleGuard('officer')],
        loadChildren: () =>
          import('./features/claims/claims.routes').then(
            (module) => module.OFFICER_CLAIM_ROUTES,
          ),
      },
      {
        path: 'my-claims',
        canActivate: [roleGuard('claimant')],
        loadChildren: () =>
          import('./features/claims/claims.routes').then(
            (module) => module.CLAIMANT_CLAIM_ROUTES,
          ),
      },
      {
        path: 'not-allowed',
        title: 'Not allowed',
        loadComponent: () =>
          import('./layout/error-pages').then((module) => module.NotAllowed),
      },
      {
        path: '**',
        title: 'Page not found',
        loadComponent: () =>
          import('./layout/error-pages').then((module) => module.NotFound),
      },
    ],
  },
];
