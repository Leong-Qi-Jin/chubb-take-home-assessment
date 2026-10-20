import { Routes } from '@angular/router';

// The list and details pages serve both roles; route data tells them which one, and the shell's role guard protects each set.
export const CLAIMANT_CLAIM_ROUTES: Routes = [
  {
    path: '',
    title: 'My claims',
    loadComponent: () =>
      import('./claims-list').then((module) => module.ClaimsList),
    data: { mode: 'claimant' },
  },
  {
    path: 'new',
    title: 'New claim',
    loadComponent: () =>
      import('./submit-claim').then((module) => module.SubmitClaim),
  },
  {
    path: ':reference',
    title: 'Claim details',
    loadComponent: () =>
      import('./claim-details').then((module) => module.ClaimDetails),
    data: { mode: 'claimant' },
  },
];

export const OFFICER_CLAIM_ROUTES: Routes = [
  {
    path: '',
    title: 'Work queue',
    loadComponent: () =>
      import('./claims-list').then((module) => module.ClaimsList),
    data: { mode: 'officer' },
  },
  {
    path: ':reference',
    title: 'Claim details',
    loadComponent: () =>
      import('./claim-details').then((module) => module.ClaimDetails),
    data: { mode: 'officer' },
  },
];
