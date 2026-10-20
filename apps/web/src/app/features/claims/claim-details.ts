import { TextFieldModule } from '@angular/cdk/text-field';
import { DatePipe } from '@angular/common';
import { Component, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatDividerModule } from '@angular/material/divider';
import { MatInputModule } from '@angular/material/input';
import { ActivatedRoute } from '@angular/router';

import { ClaimsApi } from '../../core/api/claims-api';
import {
  ClaimDetailsResponse,
  ClaimHistoryEntryResponse,
  InformationRequestResponse,
} from '../../core/api/generated';
import { Identity, ROLE_ROUTES } from '../../core/identity/identity';
import { money, MoneyPipe } from '../../shared/money';
import { Alert } from '../../shared/ui/alert';
import { confirmAction } from '../../shared/ui/confirm-dialog';
import { Icon, IconName } from '../../shared/ui/icon';
import { LoadingState } from '../../shared/ui/loading-state';
import { MoneyField } from '../../shared/ui/money-field';
import { PageHeader } from '../../shared/ui/page-header';
import { SectionHeader } from '../../shared/ui/section-header';
import { StatePanel } from '../../shared/ui/state-panel';
import { requiredText } from '../../shared/validators';
import { decisionTone } from './claim-status';
import { CLAIM_TYPES } from './claim-type';
import { ClaimTypeIcon } from './claim-type-icon';
import { StatusBadge } from './status-badge';

@Component({
  imports: [
    DatePipe,
    MatButtonModule,
    MatInputModule,
    TextFieldModule,
    ReactiveFormsModule,
    StatePanel,
    Alert,
    ClaimTypeIcon,
    Icon,
    MoneyField,
    MoneyPipe,
    StatusBadge,
    LoadingState,
    MatDividerModule,
    PageHeader,
    SectionHeader,
  ],
  selector: 'app-claim-details',
  template: `
    @if (loading()) {
      <app-loading-state message="Loading claim…" />
    } @else if (loadError()) {
      <app-state-panel
        [icon]="loadError()!.icon"
        [title]="loadError()!.title"
        [message]="loadError()!.message"
        [retryable]="loadError()!.retry"
        [backLink]="backLink"
        [backLabel]="backLabel"
        (retry)="load()"
      />
    } @else if (claim(); as item) {
      <app-page-header
        [heading]="item.reference"
        [eyebrow]="item.type + ' claim'"
        [description]="
          'Submitted ' + (item.submittedAt | date: 'd MMMM y, h:mm a')
        "
        [backLink]="backLink"
        [backLabel]="backLabel"
      >
        <app-status-badge
          headingAside
          [status]="item.status"
        />
        @if (isOfficer && item.status === 'Submitted') {
          <button
            type="button"
            matButton="filled"
            (click)="take()"
            [disabled]="busy()"
          >
            <app-icon name="user-check" />Take this claim
          </button>
        }
      </app-page-header>
      @if (justSubmitted) {
        <app-alert tone="success"
          ><strong>Claim submitted.</strong> Save this reference to track your
          claim.</app-alert
        >
      }
      @if (actionMessage()) {
        <app-alert tone="success">{{ actionMessage() }}</app-alert>
      }
      @if (actionError()) {
        <app-alert tone="danger">{{ actionError() }}</app-alert>
      }
      <div class="detail-grid">
        <div class="detail-main">
          <section class="surface incident-card">
            <app-section-header
              eyebrow="Incident"
              heading="Details of loss"
              ><app-claim-type-icon [type]="item.type"
            /></app-section-header>
            <dl class="detail-list">
              <div>
                <dt>Policy number</dt>
                <dd>{{ item.policyNumber }}</dd>
              </div>
              <div>
                <dt>Date and time</dt>
                <dd>
                  {{ item.incident.occurredAt | date: 'd MMM y, h:mm a' }}
                </dd>
              </div>
              <div>
                <dt>Incident location</dt>
                <dd>{{ item.incident.location }}</dd>
              </div>
              <div>
                <dt>
                  {{ claimTypes[item.type].detailLabel }}
                </dt>
                <dd>
                  {{
                    item.motor?.vehicleRegistration ??
                      item.property?.propertyAddress
                  }}
                </dd>
              </div>
              <div class="full">
                <dt>Description</dt>
                <dd>{{ item.incident.description }}</dd>
              </div>
            </dl>
          </section>
          @if (item.informationRequests.length) {
            <section class="surface requests-card">
              <app-section-header
                eyebrow="Communication"
                heading="Information requests"
              />
              @for (request of item.informationRequests; track request.id) {
                <article class="request-card">
                  <!-- Each message says who wrote it and when, so a request never reads as a reply and vice versa. -->
                  <app-status-badge [status]="request.status" />
                  <div class="message">
                    <span class="message-label"
                      >{{
                        isOfficer
                          ? 'Request to claimant'
                          : 'Request from your claims officer'
                      }}
                      ·
                      <time>{{
                        request.requestedAt | date: 'd MMM y, h:mm a'
                      }}</time></span
                    >
                    <p class="message-body">{{ request.requestText }}</p>
                  </div>
                  @if (request.responseText) {
                    <div class="message response">
                      <span class="message-label"
                        >{{ isOfficer ? 'Claimant response' : 'Your response' }}
                        @if (request.respondedAt) {
                          ·
                          <time>{{
                            request.respondedAt | date: 'd MMM y, h:mm a'
                          }}</time>
                        }
                      </span>
                      <p class="message-body">{{ request.responseText }}</p>
                    </div>
                  }
                  @if (request.cancellationReason) {
                    <div class="message response">
                      <span class="message-label"
                        >Request cancelled
                        @if (request.cancelledAt) {
                          ·
                          <time>{{
                            request.cancelledAt | date: 'd MMM y, h:mm a'
                          }}</time>
                        }
                      </span>
                      <p class="message-body">
                        Reason: {{ request.cancellationReason }}
                      </p>
                    </div>
                  }
                  @if (!isOfficer && request.status === 'Open') {
                    <mat-form-field class="full-width"
                      ><mat-label>Your response</mat-label
                      ><textarea
                        matInput
                        cdkTextareaAutosize
                        cdkAutosizeMinRows="4"
                        [formControl]="responseText"
                        maxlength="2000"
                      ></textarea></mat-form-field
                    ><button
                      type="button"
                      matButton="filled"
                      (click)="respond(request)"
                      [disabled]="busy()"
                    >
                      <app-icon name="send" />Send response
                    </button>
                  }
                  @if (isOfficer && request.status === 'Open' && canManage()) {
                    <div class="inline-action">
                      <mat-form-field
                        ><mat-label>Cancellation reason</mat-label
                        ><input
                          matInput
                          [formControl]="cancellationReason"
                          maxlength="500" /></mat-form-field
                      ><button
                        type="button"
                        matButton="text"
                        (click)="cancelRequest(request)"
                        [disabled]="busy()"
                      >
                        Cancel request
                      </button>
                    </div>
                  }
                </article>
              }
            </section>
          }
          @if (item.decision; as decision) {
            <section
              class="surface decision-card"
              [attr.data-tone]="decisionTone(decision.outcome)"
            >
              <app-section-header
                eyebrow="Final decision"
                [heading]="'Claim ' + decision.outcome.toLowerCase()"
              />
              <dl class="decision-details">
                @if (decision.settlementAmount) {
                  <div>
                    <dt>Settlement amount</dt>
                    <dd class="decision-amount">
                      {{ decision.settlementAmount | money }}
                    </dd>
                  </div>
                }
                <div>
                  <dt>Reason</dt>
                  <dd>{{ decision.rationale }}</dd>
                </div>
              </dl>
              <small class="decision-meta"
                >Decided by {{ decision.decidedBy.displayName }} on
                {{ decision.decidedAt | date: 'd MMMM y' }}</small
              >
            </section>
          }
          <section class="surface history-card">
            <app-section-header
              eyebrow="Activity"
              heading="Claim history"
            />
            <ol class="timeline">
              @for (
                event of history();
                track event.occurredAt + event.eventType
              ) {
                <li>
                  <span class="timeline-dot"></span>
                  <div>
                    <strong>{{ event.summary }}</strong
                    ><span
                      >{{ event.actorName }} ·
                      {{ event.occurredAt | date: 'd MMM y, h:mm a' }}</span
                    >
                  </div>
                </li>
              }
            </ol>
          </section>
        </div>
        <aside class="detail-side">
          @if (isOfficer && canManage()) {
            <section class="surface action-panel">
              <app-section-header
                eyebrow="Actions"
                heading="Progress claim"
              />
              <h3>Estimated liability</h3>
              <app-money-field
                label="Estimated amount"
                [currency]="item.currency"
                [control]="estimate"
              />
              <button
                type="button"
                matButton="outlined"
                (click)="setEstimate()"
                [disabled]="busy()"
              >
                <app-icon name="pencil" />Update estimate
              </button>
              @if (item.status === 'UnderReview') {
                <mat-divider />
                <h3>Information request</h3>
                <mat-form-field
                  class="full-width"
                  subscriptSizing="dynamic"
                  ><mat-label>Information needed</mat-label
                  ><textarea
                    matInput
                    cdkTextareaAutosize
                    cdkAutosizeMinRows="3"
                    [formControl]="requestText"
                    maxlength="2000"
                  ></textarea></mat-form-field
                ><button
                  type="button"
                  matButton="outlined"
                  (click)="requestInformation()"
                  [disabled]="busy()"
                >
                  <app-icon name="message-square" />Request information
                </button>
                <mat-divider />
                <h3>Decision</h3>
                <mat-form-field
                  class="full-width"
                  subscriptSizing="dynamic"
                  ><mat-label>Decision rationale</mat-label
                  ><textarea
                    matInput
                    cdkTextareaAutosize
                    cdkAutosizeMinRows="3"
                    [formControl]="rationale"
                    maxlength="2000"
                  ></textarea>
                </mat-form-field>
                <app-money-field
                  label="Settlement amount"
                  [currency]="item.currency"
                  [control]="settlementAmount"
                />
                <div class="decision-actions">
                  <button
                    type="button"
                    matButton="filled"
                    (click)="settle()"
                    [disabled]="busy() || !item.estimatedLiability"
                  >
                    <app-icon name="check" />Settle claim</button
                  ><button
                    type="button"
                    matButton="outlined"
                    class="danger"
                    (click)="reject()"
                    [disabled]="busy()"
                  >
                    <app-icon name="x" />Reject claim
                  </button>
                </div>
              }
            </section>
          } @else if (isOfficer && item.assignedOfficer) {
            <!-- Say exactly why the actions are unavailable: a decided claim is closed to everyone, an open claim only to officers who do not own it. -->
            <section class="surface read-only-note">
              @if (item.decision; as decision) {
                <strong>Claim closed</strong>
                <p>
                  {{
                    isMe(decision.decidedBy.id)
                      ? 'You'
                      : decision.decidedBy.displayName
                  }}
                  {{ decision.outcome === 'Settled' ? 'settled' : 'rejected' }}
                  this claim, so it can no longer be changed.
                </p>
              } @else {
                <strong>Owned by {{ item.assignedOfficer.displayName }}</strong>
                <p>
                  Only {{ item.assignedOfficer.displayName }} can update this
                  claim while it is assigned to them.
                </p>
              }
            </section>
          }
          <section class="surface summary-card">
            <app-section-header
              [eyebrow]="isOfficer ? 'Claimant' : 'Your details'"
              [heading]="item.claimant.fullName"
            />
            <dl>
              <div>
                <dt>Email</dt>
                <dd>
                  <a [href]="'mailto:' + item.claimant.email">{{
                    item.claimant.email
                  }}</a>
                </dd>
              </div>
              @if (item.claimant.phone) {
                <div>
                  <dt>Phone</dt>
                  <dd>
                    <a [href]="'tel:' + item.claimant.phone">{{
                      item.claimant.phone
                    }}</a>
                  </dd>
                </div>
              }
            </dl>
            <mat-divider />
            <dl>
              <div>
                <dt>Assigned officer</dt>
                <dd>
                  {{ item.assignedOfficer?.displayName ?? 'Not assigned' }}
                </dd>
              </div>
              @if (item.estimatedLiability) {
                <div>
                  <dt>Estimated liability</dt>
                  <dd>
                    {{ item.estimatedLiability | money }}
                  </dd>
                </div>
              }
            </dl>
          </section>
        </aside>
      </div>
    }
  `,
})
export class ClaimDetails {
  private readonly api = inject(ClaimsApi);
  private readonly route = inject(ActivatedRoute);
  private readonly identity = inject(Identity);
  private readonly dialog = inject(MatDialog);
  protected readonly isOfficer = this.route.snapshot.data['mode'] === 'officer';
  private readonly routes =
    ROLE_ROUTES[this.isOfficer ? 'officer' : 'claimant'];
  protected readonly backLink = this.routes.claims;
  protected readonly backLabel = this.routes.claimsLabel;
  protected readonly claimTypes = CLAIM_TYPES;
  protected readonly justSubmitted =
    window.history.state?.justSubmitted === true;
  protected readonly claim = signal<ClaimDetailsResponse | null>(null);
  protected readonly history = signal<ClaimHistoryEntryResponse[]>([]);
  protected readonly loading = signal(true);
  protected readonly loadError = signal<{
    icon: IconName;
    title: string;
    message: string;
    retry: boolean;
  } | null>(null);
  protected readonly busy = signal(false);
  protected readonly actionError = signal<string | null>(null);
  protected readonly actionMessage = signal<string | null>(null);
  private readonly etag = signal('*');
  private reference = '';
  protected readonly estimate = new FormControl('', {
    nonNullable: true,
    validators: [
      Validators.required,
      Validators.pattern(/^\d{1,16}(\.\d{1,2})?$/),
    ],
  });
  protected readonly requestText = new FormControl('', {
    nonNullable: true,
    validators: [requiredText, Validators.maxLength(2000)],
  });
  protected readonly responseText = new FormControl('', {
    nonNullable: true,
    validators: [requiredText, Validators.maxLength(2000)],
  });
  protected readonly cancellationReason = new FormControl('', {
    nonNullable: true,
    validators: [requiredText, Validators.maxLength(500)],
  });
  protected readonly rationale = new FormControl('', {
    nonNullable: true,
    validators: [requiredText, Validators.maxLength(2000)],
  });
  protected readonly settlementAmount = new FormControl('', {
    nonNullable: true,
    validators: [Validators.pattern(/^\d{1,16}(\.\d{1,2})?$/)],
  });
  private readonly textControls = [
    this.requestText,
    this.responseText,
    this.cancellationReason,
    this.rationale,
    this.settlementAmount,
  ];
  protected readonly decisionTone = decisionTone;
  constructor() {
    // The router reuses this component between claims, so a new reference resets the actions and reloads.
    this.route.paramMap.pipe(takeUntilDestroyed()).subscribe((params) => {
      this.reference = params.get('reference') ?? '';
      this.actionError.set(null);
      this.actionMessage.set(null);
      this.textControls.forEach((control) => control.reset());
      void this.load();
    });
    let viewerId = this.identity.user()?.id;
    // Same-role demo switches stay on this claim; reload so visibility and actions match the new caller.
    effect(() => {
      const nextId = this.identity.user()?.id;
      if (nextId === viewerId) {
        return;
      }
      viewerId = nextId;
      if (this.reference) {
        this.actionError.set(null);
        this.actionMessage.set(null);
        void this.load();
      }
    });
  }
  protected isMe(id: string | null): boolean {
    return !!id && id === this.identity.user()?.identity;
  }
  protected canManage(): boolean {
    const item = this.claim();
    const user = this.identity.user();
    return (
      !!item &&
      !!user &&
      item.assignedOfficer?.id === user.identity &&
      (item.status === 'UnderReview' || item.status === 'AwaitingInformation')
    );
  }
  protected async load(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(null);
    try {
      const [details, events] = await Promise.all([
        this.api.get(this.reference),
        this.api.history(this.reference),
      ]);
      this.claim.set(details.claim);
      this.etag.set(details.etag);
      this.history.set(events);
      this.estimate.setValue(details.claim.estimatedLiability?.amount ?? '');
    } catch (error) {
      const problem = this.api.problem(error);
      this.loadError.set(
        problem.status === 404
          ? {
              icon: 'search',
              title: 'Claim not found',
              message: 'This claim does not exist or is not available to you.',
              retry: false,
            }
          : problem.status === 403
            ? {
                icon: 'lock',
                title: 'Not allowed',
                message: 'You do not have access to this claim.',
                retry: false,
              }
            : {
                icon: 'alert-circle',
                title: 'Claim unavailable',
                message: problem.detail ?? 'Something went wrong.',
                retry: true,
              },
      );
    } finally {
      this.loading.set(false);
    }
  }
  protected take(): Promise<void> {
    return this.run('Claim assigned to you.', () =>
      this.api.take(this.reference),
    );
  }
  protected setEstimate(): Promise<void> {
    if (this.estimate.invalid)
      return this.invalid(
        'Enter a valid amount with up to two decimal places.',
        this.estimate,
      );
    return this.run('Estimated liability updated.', () =>
      this.api.updateEstimate(this.reference, this.estimate.value, this.etag()),
    );
  }
  protected requestInformation(): Promise<void> {
    if (this.requestText.invalid)
      return this.invalid('Enter the information you need.', this.requestText);
    return this.run(
      'Information request sent to the claimant.',
      () =>
        this.api.requestInformation(
          this.reference,
          this.requestText.value.trim(),
          this.etag(),
        ),
      [this.requestText],
    );
  }
  protected respond(request: InformationRequestResponse): Promise<void> {
    if (this.responseText.invalid)
      return this.invalid('Enter your response.', this.responseText);
    return this.run(
      'Response sent.',
      () =>
        this.api.respond(
          this.reference,
          request.id,
          this.responseText.value.trim(),
          this.etag(),
        ),
      [this.responseText],
    );
  }
  protected cancelRequest(request: InformationRequestResponse): Promise<void> {
    if (this.cancellationReason.invalid)
      return this.invalid(
        'Enter a cancellation reason.',
        this.cancellationReason,
      );
    return this.run(
      'Request cancelled.',
      () =>
        this.api.cancelRequest(
          this.reference,
          request.id,
          this.cancellationReason.value.trim(),
          this.etag(),
        ),
      [this.cancellationReason],
    );
  }
  protected async settle(): Promise<void> {
    // The amount is only required when settling, so Reject never asks for it.
    if (
      this.rationale.invalid ||
      !this.settlementAmount.value ||
      this.settlementAmount.invalid
    )
      return this.invalid(
        'Enter a valid settlement amount and rationale.',
        this.rationale,
        this.settlementAmount,
      );
    const claim = this.claim();
    const confirmed = await confirmAction(this.dialog, {
      title: 'Settle this claim?',
      message: `Settle ${this.reference} for ${money(this.settlementAmount.value, claim?.currency ?? '')}. This decision is final and the claimant will see it.`,
      confirmLabel: 'Settle claim',
    });
    if (!confirmed) return;
    return this.run('Claim settled.', () =>
      this.api.settle(
        this.reference,
        this.settlementAmount.value,
        this.rationale.value.trim(),
        this.etag(),
      ),
    );
  }
  protected async reject(): Promise<void> {
    if (this.rationale.invalid)
      return this.invalid('Enter a rationale for rejection.', this.rationale);
    const confirmed = await confirmAction(this.dialog, {
      title: 'Reject this claim?',
      message: `Reject ${this.reference}. This decision is final and the claimant will see it.`,
      confirmLabel: 'Reject claim',
      destructive: true,
    });
    if (!confirmed) return;
    return this.run('Claim rejected.', () =>
      this.api.reject(this.reference, this.rationale.value.trim(), this.etag()),
    );
  }
  private invalid(
    message: string,
    ...controls: FormControl<string>[]
  ): Promise<void> {
    for (const control of controls) {
      control.markAsTouched();
      // The settlement amount is optional for rejection, so it is flagged here rather than by a required validator.
      if (!control.value) control.setErrors({ required: true });
    }
    this.actionError.set(message);
    return Promise.resolve();
  }
  private async run(
    message: string,
    command: () => Promise<{ claim: ClaimDetailsResponse; etag: string }>,
    clear: FormControl<string>[] = [],
  ): Promise<void> {
    this.busy.set(true);
    this.actionError.set(null);
    this.actionMessage.set(null);
    try {
      const result = await command();
      this.claim.set(result.claim);
      this.etag.set(result.etag);
      this.actionMessage.set(message);
      // Clears the text just sent, so it cannot be sent again by mistake.
      clear.forEach((control) => control.reset());
      this.estimate.setValue(result.claim.estimatedLiability?.amount ?? '');
      // The action has already succeeded, so a failed history refresh keeps the previous history rather than reporting an error.
      this.history.set(
        await this.api.history(this.reference).catch(() => this.history()),
      );
    } catch (error) {
      const problem = this.api.problem(error);
      this.actionError.set(
        problem.detail ??
          (Object.values(problem.errors ?? {})
            .flat()
            .join(' ') ||
            'The action could not be completed.'),
      );
      // A 409 carries the current claim and ETag, so the officer can review the change and retry without reloading.
      if (problem.claim && problem.etag) {
        this.claim.set(problem.claim);
        this.etag.set(problem.etag);
        this.estimate.setValue(problem.claim.estimatedLiability?.amount ?? '');
        this.history.set(
          await this.api.history(this.reference).catch(() => this.history()),
        );
      } else if ([403, 404, 409].includes(problem.status ?? 0)) {
        try {
          const [current, events] = await Promise.all([
            this.api.get(this.reference),
            this.api.history(this.reference),
          ]);
          this.claim.set(current.claim);
          this.etag.set(current.etag);
          this.history.set(events);
        } catch {
          /* The inline error remains the useful state. */
        }
      }
    } finally {
      this.busy.set(false);
    }
  }
}
