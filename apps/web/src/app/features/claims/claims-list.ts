import { DatePipe } from '@angular/common';
import { Component, computed, inject, resource } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSelectModule } from '@angular/material/select';
import { ActivatedRoute, Params, Router, RouterLink } from '@angular/router';

import { ClaimsApi } from '../../core/api/claims-api';
import { ClaimStatus, ClaimType } from '../../core/api/generated';
import {
  DEMO_USERS,
  Identity,
  ROLE_ROUTES,
} from '../../core/identity/identity';
import { MoneyPipe } from '../../shared/money';
import { Icon } from '../../shared/ui/icon';
import { LoadingState } from '../../shared/ui/loading-state';
import { PageHeader } from '../../shared/ui/page-header';
import { StatePanel } from '../../shared/ui/state-panel';
import {
  CLAIM_STATUSES,
  CLOSED_CLAIM_STATUSES,
  OPEN_CLAIM_STATUSES,
  statusLabel,
} from './claim-status';
import { ClaimTypeIcon } from './claim-type-icon';
import { StatusBadge } from './status-badge';

/** Page sizes the claims API accepts. */
const PAGE_SIZE_OPTIONS = [5, 10, 20, 50, 100];

/** Queue status groups that are not a single `ClaimStatus`. */
const ALL_CLAIMS_STATUS = 'all';
const CLOSED_CLAIMS_STATUS = 'closed';

/** Translate the queue's status query into API `status` values. */
function statusesForQuery(
  status: string,
  isOfficer: boolean,
): ClaimStatus[] | undefined {
  if (status === ALL_CLAIMS_STATUS) {
    return undefined;
  }
  if (status === CLOSED_CLAIMS_STATUS) {
    return [...CLOSED_CLAIM_STATUSES];
  }
  if (status) {
    return [status as ClaimStatus];
  }
  return isOfficer ? [...OPEN_CLAIM_STATUSES] : undefined;
}

const QUEUE_FILTER_KEYS = ['status', 'type', 'assignedTo', 'sort'] as const;

function queueFilterStorageKey(userId: string): string {
  return `claims-queue-filters:${userId}`;
}

function readQueueFilters(userId: string): Params | null {
  try {
    const raw = localStorage.getItem(queueFilterStorageKey(userId));
    if (!raw) {
      return null;
    }
    const parsed = JSON.parse(raw) as Record<string, unknown>;
    const saved: Params = {};
    for (const key of QUEUE_FILTER_KEYS) {
      const value = parsed[key];
      if (typeof value === 'string' && value) {
        saved[key] = value;
      }
    }
    return Object.keys(saved).length ? saved : null;
  } catch {
    return null;
  }
}

@Component({
  imports: [
    DatePipe,
    MatButtonModule,
    MatPaginatorModule,
    MatSelectModule,
    RouterLink,
    StatePanel,
    ClaimTypeIcon,
    Icon,
    LoadingState,
    MoneyPipe,
    StatusBadge,
    PageHeader,
  ],
  selector: 'app-claims-list',
  styles: `
    .filter-bar {
      display: grid;
      grid-template-columns: repeat(4, minmax(0, 1fr));
      gap: 1rem;
      margin-bottom: 1rem;
      padding: 1rem;
    }
    .list-surface {
      padding: 0;
      overflow: hidden;
    }
    .list-surface mat-paginator {
      border-top: 1px solid var(--line);
    }
    .list-meta {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 0.875rem 1.5rem;
      border-bottom: 1px solid var(--line);
      color: var(--muted);
      font-size: var(--text-sm);
    }
    .list-count {
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }
    .list-meta button {
      --mat-button-text-container-height: 2rem;
      --mat-button-text-horizontal-padding: 0.5rem;
    }
    .list-meta strong {
      color: var(--ink);
      font-weight: 700;
    }
    /* Claimant rows: icon, claim, status, date/estimate, arrow. Officer rows add an owner column. */
    .claim-row {
      display: grid;
      grid-template-columns:
        auto minmax(0, 2fr) minmax(0, 1.2fr) minmax(0, 1fr)
        auto;
      align-items: center;
      gap: 1.25rem;
      padding: 1rem 1.5rem;
      border-bottom: 1px solid var(--line);
      text-decoration: none;
      transition: background 0.15s ease;
    }
    .claim-row.officer {
      grid-template-columns:
        auto minmax(0, 2fr) minmax(0, 1.2fr) minmax(0, 1fr) minmax(0, 1fr)
        auto;
    }
    .claim-row:last-of-type {
      border-bottom: 0;
    }
    .claim-row:hover {
      background: color-mix(in srgb, var(--chubb-blue) 3%, white);
    }
    .claim-primary,
    .claim-detail {
      display: flex;
      min-width: 0;
      flex-direction: column;
      gap: 0.125rem;
    }
    .claim-primary strong {
      font-size: var(--text-md);
      font-weight: 700;
    }
    .claim-detail {
      font-size: var(--text-sm);
    }
    .claim-primary span,
    .claim-detail small {
      overflow: hidden;
      color: var(--muted);
      font-size: var(--text-xs);
      text-overflow: ellipsis;
      white-space: nowrap;
    }
    .row-arrow {
      --icon-size: var(--icon-md);
      justify-self: end;
      color: var(--muted);
      transition:
        color 0.15s ease,
        transform 0.15s ease;
    }
    .claim-row:hover .row-arrow {
      color: var(--navy);
      transform: translateX(0.125rem);
    }
    .empty-list {
      display: grid;
      justify-items: center;
      gap: 0.5rem;
      padding: 4rem 1.5rem;
      text-align: center;
    }
    .empty-icon {
      width: 3rem;
      height: 3rem;
      margin-bottom: 0.5rem;
    }
    @media (max-width: 68rem) {
      .claim-row.officer {
        grid-template-columns:
          auto minmax(0, 2fr) minmax(0, 1.2fr) minmax(0, 1fr)
          auto;
      }
      .claim-row .owner {
        display: none;
      }
    }
    @media (max-width: 52rem) {
      .filter-bar {
        grid-template-columns: repeat(2, minmax(0, 1fr));
      }
      .claim-row,
      .claim-row.officer {
        grid-template-columns: auto minmax(0, 1fr) auto;
        row-gap: 0.375rem;
      }
      .claim-row app-status-badge {
        grid-column: 2;
        grid-row: 2;
      }
      .claim-row .claim-detail {
        display: none;
      }
      .row-arrow {
        grid-column: 3;
        grid-row: 1 / 3;
      }
      app-claim-type-icon {
        grid-row: 1 / 3;
      }
    }
    @media (max-width: 36rem) {
      .filter-bar {
        grid-template-columns: minmax(0, 1fr);
      }
      .claim-row {
        padding-inline: 1.25rem;
      }
    }
  `,
  template: `
    <app-page-header
      [eyebrow]="isOfficer ? 'Claims operations' : 'Claims'"
      [heading]="routes.claimsLabel"
      [description]="
        isOfficer
          ? 'Review, assess, and decide claims across the team.'
          : 'Claims you have submitted and their status.'
      "
    >
      @if (!isOfficer) {
        <a
          matButton="filled"
          routerLink="/my-claims/new"
          ><app-icon name="plus" />New claim</a
        >
      }
    </app-page-header>
    @if (isOfficer) {
      <section
        class="surface filter-bar"
        aria-label="Queue filters"
      >
        <mat-form-field subscriptSizing="dynamic"
          ><mat-label>Status</mat-label
          ><mat-select
            [value]="status()"
            (selectionChange)="setFilter('status', $event.value)"
            ><mat-option value="">All open claims</mat-option
            ><mat-option [value]="closedClaimsStatus"
              >All closed claims</mat-option
            ><mat-option [value]="allClaimsStatus">All claims</mat-option>
            @for (item of statusOptions; track item) {
              <mat-option [value]="item">{{ label(item) }}</mat-option>
            }
          </mat-select></mat-form-field
        >
        <mat-form-field subscriptSizing="dynamic"
          ><mat-label>Type</mat-label
          ><mat-select
            [value]="type()"
            (selectionChange)="setFilter('type', $event.value)"
            ><mat-option value="">All types</mat-option
            ><mat-option value="Motor">Motor</mat-option
            ><mat-option value="Property">Property</mat-option></mat-select
          ></mat-form-field
        >
        <mat-form-field subscriptSizing="dynamic"
          ><mat-label>Ownership</mat-label
          ><mat-select
            [value]="assignedTo()"
            (selectionChange)="setFilter('assignedTo', $event.value)"
            ><mat-option value="">Everyone</mat-option
            ><mat-option [value]="currentUserId()">Assigned to me</mat-option
            ><mat-option value="unassigned">Unassigned</mat-option>
            @for (officer of otherOfficers(); track officer.identity) {
              <mat-option [value]="officer.identity">{{
                officer.name
              }}</mat-option>
            }
          </mat-select></mat-form-field
        >
        <mat-form-field subscriptSizing="dynamic"
          ><mat-label>Sort</mat-label
          ><mat-select
            [value]="sort()"
            (selectionChange)="setFilter('sort', $event.value)"
            ><mat-option value="submittedAt">Oldest submitted first</mat-option
            ><mat-option value="updatedAt"
              >Recently updated first</mat-option
            ></mat-select
          ></mat-form-field
        >
      </section>
    }
    @if (claims.isLoading()) {
      <app-loading-state message="Loading claims…" />
    } @else if (claims.error(); as error) {
      <app-state-panel
        title="Claims unavailable"
        [message]="error.message"
        [retryable]="true"
        (retry)="claims.reload()"
      />
    } @else if (claims.value(); as result) {
      <section class="surface list-surface">
        @if (result.items.length) {
          <div class="list-meta">
            <span class="list-count"
              ><strong
                >{{ result.totalCount }}
                {{ result.totalCount === 1 ? 'claim' : 'claims' }}</strong
              >
              @if (hasFilters()) {
                <button
                  matButton="text"
                  type="button"
                  (click)="clearFilters()"
                >
                  <app-icon name="x" />Clear filters
                </button>
              }
            </span>
          </div>
        } @else {
          <div class="empty-list">
            <span class="icon-tile empty-icon"><app-icon name="inbox" /></span>
            <h2>No claims</h2>
            <p>
              {{
                isOfficer
                  ? 'Try a different status, type, or ownership filter.'
                  : 'You have not submitted any claims yet.'
              }}
            </p>
            @if (hasFilters()) {
              <button
                matButton="outlined"
                type="button"
                (click)="clearFilters()"
              >
                <app-icon name="x" />Clear filters
              </button>
            }
          </div>
        }
        @for (claim of result.items; track claim.reference) {
          <a
            class="claim-row"
            [class.officer]="isOfficer"
            [routerLink]="detailLink(claim.reference)"
          >
            <app-claim-type-icon [type]="claim.type" />
            <span class="claim-primary"
              ><strong>{{ claim.reference }}</strong
              ><span
                >{{ claim.type
                }}{{ isOfficer ? ' · ' + claim.claimantName : '' }}</span
              ></span
            >
            <app-status-badge [status]="claim.status" />
            @if (isOfficer) {
              <span class="claim-detail owner"
                ><small>Owner</small
                >{{ claim.assignedOfficer?.displayName ?? 'Unassigned' }}</span
              >
            }
            <span class="claim-detail"
              ><small>{{
                claim.estimatedLiability ? 'Estimate' : 'Submitted'
              }}</small
              >{{
                claim.estimatedLiability
                  ? (claim.estimatedLiability | money)
                  : (claim.submittedAt | date: 'd MMM y')
              }}</span
            >
            <app-icon
              class="row-arrow"
              name="chevron-right"
            />
          </a>
        }
        @if (result.totalCount > result.pageSize) {
          <mat-paginator
            [length]="result.totalCount"
            [pageIndex]="result.page - 1"
            [pageSize]="result.pageSize"
            [pageSizeOptions]="pageSizes"
            (page)="changePage($event)"
          />
        }
      </section>
    }
  `,
})
export class ClaimsList {
  private readonly api = inject(ClaimsApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly identity = inject(Identity);
  protected readonly currentUserId = computed(
    () => this.identity.user()?.identity ?? '',
  );
  // Officers other than the current user, for the ownership filter; "Assigned to me" covers the current user.
  protected readonly otherOfficers = computed(() =>
    DEMO_USERS.filter(
      (user) =>
        user.role === 'officer' && user.identity !== this.currentUserId(),
    ),
  );
  protected readonly isOfficer = this.route.snapshot.data['mode'] === 'officer';
  // The URL is the source of truth for filters, sort, and page, so links, reloads, and in-place navigation all show the same list.
  private readonly query = toSignal(this.route.queryParamMap, {
    initialValue: this.route.snapshot.queryParamMap,
  });
  protected readonly page = computed(() =>
    Number(this.query().get('page') ?? 1),
  );
  protected readonly pageSize = computed(() =>
    Number(this.query().get('pageSize') ?? 10),
  );
  protected readonly status = computed(() => this.query().get('status') ?? '');
  protected readonly type = computed(() => this.query().get('type') ?? '');
  // Only officers filter by owner; the API rejects assignedTo from claimants, so a claimant URL's value is ignored.
  protected readonly assignedTo = computed(
    () => (this.isOfficer && this.query().get('assignedTo')) || '',
  );
  protected readonly sort = computed(
    () => this.query().get('sort') ?? 'submittedAt',
  );
  // Group options sit above the individual statuses; empty means all open work.
  protected readonly statusOptions = CLAIM_STATUSES;
  protected readonly allClaimsStatus = ALL_CLAIMS_STATUS;
  protected readonly closedClaimsStatus = CLOSED_CLAIMS_STATUS;
  protected readonly label = statusLabel;
  protected readonly pageSizes = PAGE_SIZE_OPTIONS;
  protected readonly routes =
    ROLE_ROUTES[this.isOfficer ? 'officer' : 'claimant'];
  // Reloads whenever a filter, sort, page, or demo user changes; errors are rethrown as Error so the resource reports the API's message.
  protected readonly claims = resource({
    params: () => ({
      query: {
        page: this.page(),
        pageSize: this.pageSize(),
        status: statusesForQuery(this.status(), this.isOfficer),
        type: (this.type() || undefined) as ClaimType | undefined,
        // The API rejects an empty assignedTo, and claimants may not send it at all.
        assignedTo: this.assignedTo() || undefined,
        sort: this.sort(),
        // Officers work oldest submissions first; recent updates and the claimant's own list show newest first.
        order:
          this.isOfficer && this.sort() === 'submittedAt'
            ? ('asc' as const)
            : ('desc' as const),
      },
      viewer: this.currentUserId(),
    }),
    loader: async ({ params }) => {
      try {
        return await this.api.list(params.query);
      } catch (error) {
        throw new Error(this.api.problemMessage(error), { cause: error });
      }
    },
  });
  constructor() {
    // Bare /queue restores this officer's last filters. Dashboard links with query params win.
    if (!this.isOfficer) {
      return;
    }
    const query = this.route.snapshot.queryParamMap;
    if (QUEUE_FILTER_KEYS.some((key) => query.has(key))) {
      return;
    }
    const userId = this.identity.user()?.id;
    const saved = userId ? readQueueFilters(userId) : null;
    if (saved) {
      void this.navigate(saved);
    }
  }
  /** True when a filter narrows the queue, such as after opening it from a dashboard total. */
  protected readonly hasFilters = computed(
    () => !!(this.status() || this.type() || this.assignedTo()),
  );
  protected clearFilters(): void {
    void this.navigate({ status: null, type: null, assignedTo: null, page: 1 });
  }
  protected detailLink(reference: string): string {
    return `${this.routes.claims}/${reference}`;
  }
  protected setFilter(
    name: 'status' | 'type' | 'assignedTo' | 'sort',
    value: string,
  ): void {
    void this.navigate({ [name]: value || null, page: 1 });
  }
  protected changePage(event: PageEvent): void {
    void this.navigate({
      page: event.pageIndex + 1,
      pageSize: event.pageSize,
    });
  }
  private async navigate(queryParams: Params): Promise<void> {
    await this.router.navigate([], {
      relativeTo: this.route,
      queryParams,
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
    this.persistQueueFilters(queryParams);
  }

  private persistQueueFilters(update: Params): void {
    if (!this.isOfficer) {
      return;
    }
    const userId = this.identity.user()?.id;
    if (!userId) {
      return;
    }
    const current: Record<(typeof QUEUE_FILTER_KEYS)[number], string> = {
      status: this.status(),
      type: this.type(),
      assignedTo: this.assignedTo(),
      sort: this.sort() === 'submittedAt' ? '' : this.sort(),
    };
    const stored: Record<string, string> = {};
    for (const key of QUEUE_FILTER_KEYS) {
      const next =
        key in update ? ((update[key] as string | null) ?? '') : current[key];
      if (next) {
        stored[key] = next;
      }
    }
    const storageKey = queueFilterStorageKey(userId);
    if (Object.keys(stored).length) {
      localStorage.setItem(storageKey, JSON.stringify(stored));
    } else {
      localStorage.removeItem(storageKey);
    }
  }
}
