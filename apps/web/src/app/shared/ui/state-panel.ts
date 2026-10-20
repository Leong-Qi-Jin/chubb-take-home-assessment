import { Component, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { RouterLink } from '@angular/router';

import { Icon, IconName } from './icon';

@Component({
  imports: [Icon, MatButtonModule, RouterLink],
  selector: 'app-state-panel',
  template: `
    <section
      class="state-panel"
      role="alert"
    >
      <span class="icon-tile state-icon"><app-icon [name]="icon()" /></span>
      <h1>{{ title() }}</h1>
      <p>{{ message() }}</p>
      <div class="state-actions">
        @if (retryable()) {
          <button
            type="button"
            matButton="filled"
            (click)="retry.emit()"
          >
            <app-icon name="rotate-ccw" />Try again
          </button>
        }
        @if (backLink(); as link) {
          <!-- The way back is secondary when a retry is offered, otherwise it is the only action. -->
          <a
            [matButton]="retryable() ? 'outlined' : 'filled'"
            [routerLink]="link"
            ><app-icon name="chevron-left" />{{ backLabel() }}</a
          >
        }
      </div>
    </section>
  `,
})
export class StatePanel {
  readonly icon = input<IconName>('alert-circle');
  readonly title = input.required<string>();
  readonly message = input.required<string>();
  readonly retryable = input(false);
  readonly backLink = input<string | null>(null);
  readonly backLabel = input('');
  readonly retry = output<void>();
}
