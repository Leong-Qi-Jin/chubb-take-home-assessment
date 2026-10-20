import { computed, Injectable, signal } from '@angular/core';

export type UserRole = 'claimant' | 'officer';
export interface DemoUser {
  id: string;
  name: string;
  role: UserRole;
  identity: string;
}

export const DEMO_USERS: DemoUser[] = [
  {
    id: 'alex',
    name: 'Alex Tan',
    role: 'claimant',
    identity: 'alex.tan@example.com',
  },
  {
    id: 'maya',
    name: 'Maya Lim',
    role: 'claimant',
    identity: 'maya.lim@example.com',
  },
  {
    id: 'sam',
    name: 'Sam Lee',
    role: 'officer',
    identity: '00000000-0000-0000-0000-000000000101',
  },
  {
    id: 'aisha',
    name: 'Aisha Khan',
    role: 'officer',
    identity: '00000000-0000-0000-0000-000000000102',
  },
  {
    id: 'benjamin',
    name: 'Benjamin Tan',
    role: 'officer',
    identity: '00000000-0000-0000-0000-000000000103',
  },
];

/** Where each role lands and where its claims live, shared by navigation, links, and back links. */
export const ROLE_ROUTES: Record<
  UserRole,
  { home: string; claims: string; claimsLabel: string }
> = {
  claimant: {
    home: '/my-claims',
    claims: '/my-claims',
    claimsLabel: 'My claims',
  },
  officer: { home: '/dashboard', claims: '/queue', claimsLabel: 'Work queue' },
};

@Injectable({ providedIn: 'root' })
export class Identity {
  private readonly selectedId = signal(localStorage.getItem('claims-user'));
  // A user is always selected: a missing or unknown stored id falls back to the first demo user, so every request carries an identity.
  readonly user = computed(
    () =>
      DEMO_USERS.find((user) => user.id === this.selectedId()) ?? DEMO_USERS[0],
  );
  readonly role = computed(() => this.user()?.role ?? null);

  select(id: string): void {
    this.selectedId.set(id);
    localStorage.setItem('claims-user', id);
  }
}
