import { Component, computed, input } from '@angular/core';

import { ClaimType } from '../../core/api/generated';
import { Icon } from '../../shared/ui/icon';
import { CLAIM_TYPES } from './claim-type';

/** Tinted tile with the claim type's icon. Decorative: the type is always also shown as text nearby. */
@Component({
  imports: [Icon],
  selector: 'app-claim-type-icon',
  host: {
    class: 'icon-tile claim-type-icon',
    '[class]': 'meta().tileClass',
    'aria-hidden': 'true',
  },
  styles: `
    :host {
      width: 2.5rem;
      height: 2.5rem;
    }
    :host(.property) {
      color: var(--chubb-purple);
      background: var(--purple-tint);
    }
  `,
  template: `<app-icon [name]="meta().icon" />`,
})
export class ClaimTypeIcon {
  readonly type = input.required<ClaimType>();
  protected readonly meta = computed(() => CLAIM_TYPES[this.type()]);
}
