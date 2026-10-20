import { Component, computed, input } from '@angular/core';

import { Icon, IconName } from './icon';
import { Tone } from './tone';

const TONE_ICONS: Record<Tone, IconName> = {
  brand: 'alert-circle',
  info: 'alert-circle',
  warning: 'alert-circle',
  success: 'check',
  danger: 'alert-circle',
  neutral: 'alert-circle',
};

/** Inline message in one of the shared tones. Project the message as content and an optional action with the `alertAction` attribute. Danger alerts interrupt screen readers; others are polite. */
@Component({
  imports: [Icon],
  selector: 'app-alert',
  host: {
    class: 'alert',
    '[attr.data-tone]': 'tone()',
    '[attr.role]': "tone() === 'danger' ? 'alert' : 'status'",
  },
  template: `
    <app-icon [name]="icon()" />
    <span class="alert-message"><ng-content /></span>
    <ng-content select="[alertAction]" />
  `,
})
export class Alert {
  readonly tone = input<Tone>('info');
  protected readonly icon = computed(() => TONE_ICONS[this.tone()]);
}
