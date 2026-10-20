import { ClaimType } from '../../core/api/generated';
import { IconName } from '../../shared/ui/icon';

/** Presentation of each claim type: its icon, its tile modifier, and the label of the detail that identifies what was damaged. */
export const CLAIM_TYPES: Record<
  ClaimType,
  { icon: IconName; tileClass: string; detailLabel: string }
> = {
  Motor: { icon: 'car', tileClass: '', detailLabel: 'Vehicle registration' },
  Property: {
    icon: 'house',
    tileClass: 'property',
    detailLabel: 'Property address',
  },
};
