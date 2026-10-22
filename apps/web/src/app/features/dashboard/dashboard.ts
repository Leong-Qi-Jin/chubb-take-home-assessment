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
  styles: `
    .metrics {
      display: grid;
      grid-template-columns: repeat(6, minmax(0, 1fr));
      gap: 1rem;
      margin-bottom: 1rem;
    }
    .metric-card {
      display: flex;
      min-height: 8.5rem;
      flex-direction: column;
      gap: 0.5rem;
      padding: 1.25rem;
      color: inherit;
      text-decoration: none;
      transition:
        border-color 0.15s ease,
        background-color 0.15s ease;
    }
    /* Labels reserve two lines so every metric number sits on the same baseline. */
    .metric-card > span {
      min-height: 2.6em;
      color: var(--muted);
      font-size: var(--text-sm);
      font-weight: 700;
      line-height: 1.3;
    }
    /* A tone dot beside plain text marks the status without a badge box, so long labels wrap like normal text. */
    .metric-label {
      display: flex;
      align-items: flex-start;
      gap: 0.5rem;
    }
    /* Centred on the first line of the label, so it stays put when the label wraps. */
    .status-dot {
      width: 0.5rem;
      height: 0.5rem;
      flex-shrink: 0;
      margin-top: calc((1.3em - 0.5rem) / 2);
      border-radius: 50%;
      background: var(--tone-fg);
    }
    .metric-card small {
      margin-top: auto;
      color: var(--muted);
      font-size: var(--text-xs);
    }
    .metric-card strong {
      font-size: var(--text-metric);
      line-height: 1;
    }
    /* Metric cards are links into the filtered queue and use Chubb purple on hover, the same hover colour as buttons. White cards take a light purple tint so the status dots stay readable. */
    .metric-card:hover {
      border-color: color-mix(in srgb, var(--chubb-purple) 40%, white);
      background: var(--purple-tint);
    }
    .metric-card.accent {
      color: #fff;
      border-color: var(--navy);
      background: var(--navy);
    }
    /* The blue "My open claims" card is a filled element, so it behaves like a filled button: its fill turns Chubb purple on hover. */
    .metric-card.accent:hover {
      border-color: var(--chubb-purple);
      background: var(--chubb-purple);
    }
    .metric-card.accent > span,
    .metric-card.accent small {
      color: rgb(255 255 255 / 0.78);
    }
    /* Equal two-column layouts. */
    .dashboard-grid,
    .liability-list {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(12rem, 1fr));
      gap: 1rem;
    }
    .liability-list div {
      display: flex;
      flex-direction: column;
      gap: 0.375rem;
      padding: 1.25rem;
      border-radius: var(--radius-md);
      background: var(--navy-tint);
    }
    .liability-list span {
      color: var(--navy);
      font-size: var(--text-xs);
      font-weight: 700;
    }
    .liability-list strong {
      font-size: var(--text-xl);
    }
    .workload-list {
      display: grid;
      gap: 1rem;
    }
    .workload-row {
      display: grid;
      grid-template-columns: auto minmax(0, 1fr) auto;
      align-items: center;
      column-gap: 0.75rem;
      row-gap: 0.375rem;
      padding-block: 0.25rem;
      color: inherit;
      text-decoration: none;
      font-size: var(--text-sm);
    }
    .workload-row:hover .workload-officer {
      text-decoration: underline;
    }
    .avatar {
      grid-row: 1 / span 2;
      width: 2.25rem;
      height: 2.25rem;
      border-radius: 50%;
      font-size: var(--text-sm);
      font-weight: 700;
    }
    .workload-name {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      min-width: 0;
    }
    .workload-row strong {
      font-weight: 700;
    }
    /* Single series, so one brand hue; the bar is square like every other shape in the system. */
    .workload-bar {
      grid-column: 2 / -1;
      height: 0.5rem;
    }
    .workload-bar span {
      display: block;
      min-width: 0.25rem;
      height: 100%;
      border-radius: var(--radius-sm);
      background: var(--navy);
    }
    .empty-copy {
      font-size: var(--text-sm);
    }
    @media (max-width: 68rem) {
      .metrics {
        grid-template-columns: repeat(3, minmax(0, 1fr));
      }
    }
    @media (max-width: 52rem) {
      .dashboard-grid {
        grid-template-columns: minmax(0, 1fr);
      }
    }
    @media (max-width: 36rem) {
      .metrics {
        grid-template-columns: repeat(2, minmax(0, 1fr));
        gap: 0.75rem;
      }
      .metric-card {
        min-height: 7.5rem;
        padding: 1rem;
      }
    }
  `,
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
          ><strong class="headline-figure">{{ dashboard.myOpenClaims }}</strong
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
            ><strong class="headline-figure">{{
              dashboard.countsByStatus[item]
            }}</strong
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
                  ><strong class="headline-figure">{{ item | money }}</strong>
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
  private readonly identity = inject(Identity);
  // Reloads when the demo officer changes so "my open claims" matches the new caller.
  protected readonly data = resource({
    params: () => this.identity.user()?.identity,
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
