import {
  ClaimDecisionOutcome,
  ClaimStatus,
  InformationRequestStatus,
} from '../../core/api/generated';
import { Tone } from '../../shared/ui/tone';

/** Every claim status in workflow order, as shown on the dashboard. */
export const CLAIM_STATUSES: readonly ClaimStatus[] = [
  'Submitted',
  'UnderReview',
  'AwaitingInformation',
  'Settled',
  'Rejected',
];

/** Statuses an officer can still act on; the work queue shows these by default. */
export const OPEN_CLAIM_STATUSES: readonly ClaimStatus[] = [
  'Submitted',
  'UnderReview',
  'AwaitingInformation',
];

/** Terminal statuses; the queue "All closed claims" filter uses these. */
export const CLOSED_CLAIM_STATUSES: readonly ClaimStatus[] = [
  'Settled',
  'Rejected',
];

const STATUS_LABELS: Record<ClaimStatus, string> = {
  Submitted: 'Submitted',
  UnderReview: 'Under review',
  AwaitingInformation: 'Awaiting information',
  Settled: 'Settled',
  Rejected: 'Rejected',
};

export function statusLabel(status: ClaimStatus): string {
  return STATUS_LABELS[status];
}

const CLAIM_STATUS_TONES: Record<ClaimStatus, Tone> = {
  Submitted: 'info',
  UnderReview: 'brand',
  AwaitingInformation: 'warning',
  Settled: 'success',
  Rejected: 'danger',
};

export function claimStatusTone(status: ClaimStatus): Tone {
  return CLAIM_STATUS_TONES[status];
}

const REQUEST_STATUS_TONES: Record<InformationRequestStatus, Tone> = {
  Open: 'warning',
  Answered: 'success',
  Cancelled: 'neutral',
};

export function requestStatusTone(status: InformationRequestStatus): Tone {
  return REQUEST_STATUS_TONES[status];
}

const REQUEST_STATUS_LABELS: Record<InformationRequestStatus, string> = {
  Open: 'Open',
  Answered: 'Answered',
  Cancelled: 'Cancelled',
};

export function requestStatusLabel(status: InformationRequestStatus): string {
  return REQUEST_STATUS_LABELS[status];
}

const DECISION_TONES: Record<ClaimDecisionOutcome, Tone> = {
  Settled: 'success',
  Rejected: 'danger',
};

export function decisionTone(outcome: ClaimDecisionOutcome): Tone {
  return DECISION_TONES[outcome];
}
