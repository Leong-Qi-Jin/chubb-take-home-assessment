import {
  Component,
  computed,
  ElementRef,
  inject,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import {
  NavigationEnd,
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet,
} from '@angular/router';
import { filter, map } from 'rxjs';

import { DEMO_USERS, Identity, ROLE_ROUTES } from '../core/identity/identity';
import { Icon } from '../shared/ui/icon';

@Component({
  imports: [
    Icon,
    MatButtonModule,
    MatSelectModule,
    RouterLink,
    RouterLinkActive,
    RouterOutlet,
  ],
  selector: 'app-shell',
  template: `
    <!-- The skip link moves focus programmatically: a plain fragment link would change the URL and trigger router navigation. -->
    <a
      class="skip-link"
      href="#main-content"
      (click)="focusMain($event)"
      >Skip to main content</a
    >
    <header class="app-header">
      <a
        class="brand"
        [routerLink]="homeLink()"
        aria-label="Claims home"
      >
        <span class="brand-mark">C</span>
        <span>Claim<span class="brand-muted">Track</span></span>
      </a>
      <nav aria-label="Main navigation">
        @if (identity.role() === 'claimant') {
          <!-- "My claims" also covers claim details, but not the new-claim page, which has its own item. -->
          <a
            matButton="text"
            routerLink="/my-claims"
            [class.active]="myClaimsActive()"
            [attr.aria-current]="myClaimsActive() ? 'page' : null"
            ><app-icon name="file-text" />My claims</a
          >
          <a
            matButton="text"
            routerLink="/my-claims/new"
            routerLinkActive="active"
            ariaCurrentWhenActive="page"
            ><app-icon name="plus" />New claim</a
          >
        } @else {
          <a
            matButton="text"
            routerLink="/dashboard"
            routerLinkActive="active"
            ariaCurrentWhenActive="page"
            ><app-icon name="layout-dashboard" />Dashboard</a
          >
          <a
            matButton="text"
            routerLink="/queue"
            routerLinkActive="active"
            ariaCurrentWhenActive="page"
            ><app-icon name="list" />Work queue</a
          >
        }
      </nav>
      <div class="user-switcher">
        <mat-form-field subscriptSizing="dynamic">
          <mat-select
            aria-label="Switch demo user"
            panelClass="user-switcher-panel"
            [value]="identity.user()?.id"
            (selectionChange)="switchUser($event.value)"
          >
            <mat-select-trigger
              >{{ identity.user()?.name
              }}<span class="option-meta">{{
                identity.role()
              }}</span></mat-select-trigger
            >
            @for (group of userGroups; track group.label) {
              <mat-optgroup [label]="group.label">
                @for (user of group.users; track user.id) {
                  <mat-option [value]="user.id">{{ user.name }}</mat-option>
                }
              </mat-optgroup>
            }
          </mat-select>
        </mat-form-field>
      </div>
    </header>
    <main
      #main
      id="main-content"
      class="page-shell"
      tabindex="-1"
    >
      <router-outlet />
    </main>
    <footer class="app-footer">ClaimTrack · Demo data</footer>
  `,
})
export class AppShell {
  protected readonly identity = inject(Identity);
  protected readonly userGroups = [
    {
      label: 'Claimants',
      users: DEMO_USERS.filter((user) => user.role === 'claimant'),
    },
    {
      label: 'Claims officers',
      users: DEMO_USERS.filter((user) => user.role === 'officer'),
    },
  ];
  private readonly router = inject(Router);
  private readonly url = toSignal(
    this.router.events.pipe(
      filter((event) => event instanceof NavigationEnd),
      map(() => this.router.url),
    ),
    { initialValue: this.router.url },
  );
  protected readonly myClaimsActive = computed(() => {
    const path = this.url().split('?')[0];
    return path.startsWith('/my-claims') && path !== '/my-claims/new';
  });
  private readonly main = viewChild.required<ElementRef<HTMLElement>>('main');

  constructor() {
    // Move focus to the new page after in-app navigation so keyboard and screen reader users start at its content. Query-only changes, such as queue filters, keep focus where it is.
    let previousPath = this.router.url.split('?')[0];
    this.router.events
      .pipe(
        filter((event) => event instanceof NavigationEnd),
        map(() => this.router.url.split('?')[0]),
        filter((path) => path !== previousPath),
        takeUntilDestroyed(),
      )
      .subscribe((path) => {
        previousPath = path;
        this.main().nativeElement.focus({ preventScroll: true });
      });
  }

  protected focusMain(event: Event): void {
    event.preventDefault();
    this.main().nativeElement.focus();
  }

  protected homeLink(): string {
    return ROLE_ROUTES[this.identity.role() ?? 'claimant'].home;
  }
  protected switchUser(id: string): void {
    const previousRole = this.identity.role();
    this.identity.select(id);
    // Same-role switches stay on the current page so queue filters and open claims remain. A role change has to leave: the other role's routes are guarded.
    if (this.identity.role() !== previousRole) {
      void this.router.navigateByUrl(this.homeLink());
    }
  }
}
