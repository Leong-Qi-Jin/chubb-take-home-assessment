import { Component, computed, inject, resource } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { RouterLink } from '@angular/router';

import { ClaimsApi } from '../../core/api/claims-api';
import { Identity } from '../../core/identity/identity';
import { currencyName } from '../../shared/money';
import { MoneyPipe } from '../../shared/money';
import { Badge } from '../../shared/ui/badge';
import { Icon } from '../../shared/ui/icon';
import { LoadingState } from '../../shared/ui/loading-state';
import { PageHeader } from '../../shared/ui/page-header';
import { SectionHeader } from '../../shared/ui/section-header';
import { StatePanel } from '../../shared/ui/state-panel';
import {
  CLAIM_STATUSES,
  claimStatusTone,
  statusLabel,
} from '../claims/claim-status';

@Component({
  imports: [
    Badge,
    Icon,
    LoadingState,
    MatButtonModule,
    MoneyPipe,
    PageHeader,
    RouterLink,
    SectionHeader,
    StatePanel,
  ],
  selector: 'app-dashboard',
  template: `
    <app-page-header
      eyebrow="Claims operations"
      heading="Dashboard"
      description="Claim counts, workload, and outstanding liability for the team."
    >
      <a
        matButton="filled"
        routerLink="/queue"
        ><app-icon name="list" />Open work queue</a
      >
    </app-page-header>
    @if (data.isLoading()) {
      <app-loading-state message="Loading dashboard…" />
    } @else if (data.error(); as error) {
      <app-state-panel
        title="Dashboard unavailable"
        [message]="error.message"
        [retryable]="true"
        (retry)="data.reload()"
      />
    } @else if (data.value(); as dashboard) {
      <section
        class="metrics"
        aria-label="Claim totals"
      >
        <!-- Each total opens the work queue with the matching filter. -->
        <a
          class="surface metric-card accent"
          routerLink="/queue"
          [queryParams]="{ assignedTo: myId() }"
        >
          <span>My open claims</span
          ><strong>{{ dashboard.myOpenClaims }}</strong
          ><small>Assigned to you</small>
        </a>
        @for (item of statuses; track item) {
          <a
            class="surface metric-card"
            routerLink="/queue"
            [queryParams]="{ status: item }"
          >
            <span class="metric-label"
              ><span
                class="status-dot"
                [attr.data-tone]="tone(item)"
              ></span
              >{{ label(item) }}</span
            ><strong>{{ dashboard.countsByStatus[item] }}</strong
            ><small>Across the team</small>
          </a>
        }
      </section>
      <div class="dashboard-grid">
        <section class="surface">
          <app-section-header
            eyebrow="Financial overview"
            heading="Outstanding liability"
          />
          @if (dashboard.outstandingLiability.length) {
            <div class="liability-list">
              @for (
                item of dashboard.outstandingLiability;
                track item.currency
              ) {
                <div>
                  <span>{{ currencyOf(item.currency) }}</span
                  ><strong>{{ item | money }}</strong>
                </div>
              }
            </div>
          } @else {
            <p class="empty-copy">No estimated liability on open claims.</p>
          }
        </section>
        <section class="surface">
          <app-section-header
            eyebrow="Team"
            heading="Open claims by officer"
          />
          @if (workload().length) {
            <!-- Bars share one baseline and scale to the busiest officer, so relative workload reads at a glance; the count stays as text. -->
            <ul class="workload-list">
              @for (row of workload(); track row.officer.id) {
                <li>
                  <a
                    class="workload-row"
                    routerLink="/queue"
                    [queryParams]="{ assignedTo: row.officer.id }"
                    [attr.aria-label]="
                      row.officer.displayName +
                      (row.isMe ? ' (you)' : '') +
                      ', ' +
                      row.count +
                      (row.count === 1 ? ' open claim' : ' open claims')
                    "
                  >
                    <span
                      class="icon-tile avatar"
                      aria-hidden="true"
                      >{{ row.officer.displayName.charAt(0) }}</span
                    >
                    <span
                      class="workload-name"
                      aria-hidden="true"
                      ><span class="workload-officer">{{
                        row.officer.displayName
                      }}</span>
                      @if (row.isMe) {
                        <app-badge tone="brand">You</app-badge>
                      }
                    </span>
                    <strong aria-hidden="true">{{ row.count }}</strong>
                    <span
                      class="workload-bar"
                      aria-hidden="true"
                      ><span [style.width.%]="row.share"></span
                    ></span>
                  </a>
                </li>
              }
            </ul>
          } @else {
            <p class="empty-copy">No open claims are assigned to officers.</p>
          }
        </section>
      </div>
    }
  `,
})
export class Dashboard {
  private readonly api = inject(ClaimsApi);
  // Errors are rethrown as Error so the resource reports the API's message.
  protected readonly data = resource({
    loader: async () => {
      try {
        return await this.api.dashboard();
      } catch (error) {
        throw new Error(this.api.problemMessage(error), { cause: error });
      }
    },
  });
  protected readonly statuses = CLAIM_STATUSES;
  protected readonly label = statusLabel;
  protected readonly tone = claimStatusTone;
  protected readonly currencyOf = currencyName;
  private readonly identity = inject(Identity);
  protected readonly myId = computed(() => this.identity.user()?.identity);
  // Busiest officer first; each bar's length is its share of the busiest officer's count.
  protected readonly workload = computed(() => {
    const rows = this.data.value()?.openClaimsByOfficer ?? [];
    const max = Math.max(1, ...rows.map((row) => row.count));
    const me = this.myId();
    return [...rows]
      .sort(
        (a, b) =>
          b.count - a.count ||
          a.officer.displayName.localeCompare(b.officer.displayName),
      )
      .map((row) => ({
        ...row,
        isMe: row.officer.id === me,
        share: (row.count / max) * 100,
      }));
  });
}
