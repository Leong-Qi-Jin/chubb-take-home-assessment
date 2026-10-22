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
  styles: `
    :host {
      display: flex;
      align-items: center;
      gap: 0.625rem;
      margin-bottom: 1rem;
      padding: 0.875rem 1rem;
      border: 1px solid var(--tone-border);
      border-radius: var(--radius-md);
      color: var(--tone-fg);
      background: var(--tone-bg);
      font-size: var(--text-sm);
      /* An inline action is projected from the page, so it takes the tone through these inherited button tokens. */
      --mat-button-text-label-text-color: var(--tone-fg);
      --mat-button-text-state-layer-color: var(--tone-fg);
      --mat-button-text-container-height: 2rem;
    }
    app-icon {
      --icon-size: var(--icon-md);
    }
    /* A message that sits beside an inline action takes the remaining width and pushes the action to the end. */
    .alert-message {
      flex: 1;
    }
  `,
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
