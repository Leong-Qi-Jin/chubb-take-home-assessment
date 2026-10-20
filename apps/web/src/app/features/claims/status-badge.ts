import { Component, computed, input } from '@angular/core';

import {
  ClaimStatus,
  InformationRequestStatus,
} from '../../core/api/generated';
import {
  claimStatusTone,
  requestStatusLabel,
  requestStatusTone,
  statusLabel,
} from './claim-status';

const REQUEST_STATUSES: readonly string[] = ['Open', 'Answered', 'Cancelled'];

/** Badge for a claim or information-request status, with the label and tone from the shared mappings. The two status sets share no names, so one input covers both. */
@Component({
  selector: 'app-status-badge',
  host: { class: 'badge', '[attr.data-tone]': 'tone()' },
  template: `{{ label() }}`,
})
export class StatusBadge {
  readonly status = input.required<ClaimStatus | InformationRequestStatus>();
  private readonly isRequest = computed(() =>
    REQUEST_STATUSES.includes(this.status()),
  );
  protected readonly label = computed(() =>
    this.isRequest()
      ? requestStatusLabel(this.status() as InformationRequestStatus)
      : statusLabel(this.status() as ClaimStatus),
  );
  protected readonly tone = computed(() =>
    this.isRequest()
      ? requestStatusTone(this.status() as InformationRequestStatus)
      : claimStatusTone(this.status() as ClaimStatus),
  );
}
