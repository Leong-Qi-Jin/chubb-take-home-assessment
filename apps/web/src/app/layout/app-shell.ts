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
  styles: `
    /* The shell fills the viewport and the main region takes the remaining height, so short pages keep the footer at the bottom without scrolling. */
    :host {
      display: flex;
      min-height: 100dvh;
      flex-direction: column;
    }
    .skip-link {
      position: absolute;
      z-index: 20;
      top: 0.75rem;
      left: 0.75rem;
      padding: 0.5rem 1rem;
      border-radius: var(--radius-sm);
      color: var(--navy);
      background: #fff;
      font-size: var(--text-sm);
      font-weight: 700;
      text-decoration: none;
      transform: translateY(-200%);
    }
    .skip-link:focus {
      transform: none;
      box-shadow: var(--shadow-overlay);
    }
    .app-header {
      position: sticky;
      z-index: 10;
      top: 0;
      display: grid;
      grid-template-columns: minmax(0, 1fr) auto minmax(0, 1fr);
      align-items: center;
      gap: 1rem;
      min-height: 4.5rem;
      padding: 0 var(--page-inline);
      color: var(--ink);
      border-bottom: 1px solid var(--line-strong);
      background: var(--surface);
    }
    .brand {
      display: inline-flex;
      align-items: center;
      gap: 0.625rem;
      width: max-content;
      color: var(--ink);
      font-family: var(--font-serif);
      font-size: var(--text-xl);
      font-weight: 600;
      text-decoration: none;
      letter-spacing: -0.01em;
    }
    .brand-mark {
      display: grid;
      width: 2rem;
      height: 2rem;
      place-items: center;
      border-radius: var(--radius-sm);
      color: #fff;
      background: var(--navy);
      font-family: var(--font-sans);
      font-size: var(--text-md);
      font-weight: 700;
    }
    .brand-muted {
      color: var(--navy);
    }
    /* Navigation items run the full header height, so the current page's black block reaches both edges. */
    nav {
      display: flex;
      align-self: stretch;
    }
    /* Navigation follows chubb.com: black bold links whose text underlines on hover (like every button), with the current page as a solid black block. */
    nav a {
      height: auto;
      --mat-button-text-container-height: 100%;
      --mat-button-text-label-text-color: var(--ink);
      --mat-button-text-state-layer-color: var(--ink);
      --mat-button-text-hover-state-layer-opacity: 0;
    }
    nav a.active {
      --mat-button-text-label-text-color: #fff;
      --mat-button-text-state-layer-color: #fff;
      background: var(--ink);
    }
    /* User switcher: a compact control in the header. */
    .user-switcher {
      justify-self: end;
    }
    .user-switcher mat-form-field {
      width: 12.5rem;
      --mat-form-field-container-height: 2.5rem;
      --mat-form-field-container-vertical-padding: 0.5rem;
      --mat-form-field-outlined-focus-outline-width: 0.0625rem;
      --mat-select-trigger-text-size: var(--text-sm);
    }
    .option-meta {
      margin-left: 0.5rem;
      color: var(--muted);
      font-size: var(--text-xs);
      text-transform: capitalize;
    }
    .page-shell {
      flex: 1;
      padding: 2.5rem var(--page-inline) 3.5rem;
    }
    .app-footer {
      padding: 1.5rem var(--page-inline);
      border-top: 1px solid var(--line);
      color: var(--muted);
      text-align: center;
      font-size: var(--text-xs);
    }
    @media (max-width: 52rem) {
      .app-header {
        grid-template-columns: minmax(0, 1fr) auto;
      }
      nav {
        position: fixed;
        z-index: 10;
        right: 0;
        bottom: 0;
        left: 0;
        justify-content: center;
        height: 3.5rem;
        padding: 0 var(--gutter);
        border-top: 1px solid var(--line-strong);
        background: var(--surface);
      }
      /* Only the last element needs room for the fixed bottom navigation. */
      .app-footer {
        padding-bottom: 5rem;
      }
    }
    @media (max-width: 36rem) {
      .brand > span:last-child {
        display: none;
      }
      .user-switcher mat-form-field {
        width: 10.5rem;
      }
    }
  `,
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
