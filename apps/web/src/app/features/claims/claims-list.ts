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
  OPEN_CLAIM_STATUSES,
  statusLabel,
} from './claim-status';
import { ClaimTypeIcon } from './claim-type-icon';
import { StatusBadge } from './status-badge';

/** Page sizes the claims API accepts. */
const PAGE_SIZE_OPTIONS = [5, 10, 20, 50, 100];

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
            ><mat-option value="">All open claims</mat-option>
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
  // The status filter offers every status; with none chosen, the queue shows only open work.
  protected readonly statusOptions = CLAIM_STATUSES;
  protected readonly openStatuses = OPEN_CLAIM_STATUSES;
  protected readonly label = statusLabel;
  protected readonly pageSizes = PAGE_SIZE_OPTIONS;
  protected readonly routes =
    ROLE_ROUTES[this.isOfficer ? 'officer' : 'claimant'];
  // Reloads whenever a filter, sort, or page signal changes; errors are rethrown as Error so the resource reports the API's message.
  protected readonly claims = resource({
    params: () => ({
      page: this.page(),
      pageSize: this.pageSize(),
      status: this.status()
        ? [this.status() as ClaimStatus]
        : this.isOfficer
          ? [...this.openStatuses]
          : undefined,
      type: (this.type() || undefined) as ClaimType | undefined,
      // The API rejects an empty assignedTo, and claimants may not send it at all.
      assignedTo: this.assignedTo() || undefined,
      sort: this.sort(),
      // Officers work oldest submissions first; recent updates and the claimant's own list show newest first.
      order:
        this.isOfficer && this.sort() === 'submittedAt'
          ? ('asc' as const)
          : ('desc' as const),
    }),
    loader: async ({ params }) => {
      try {
        return await this.api.list(params);
      } catch (error) {
        throw new Error(this.api.problemMessage(error), { cause: error });
      }
    },
  });
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
  }
}
