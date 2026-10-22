import { Component, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { RouterLink } from '@angular/router';

import { Icon, IconName } from './icon';

@Component({
  imports: [Icon, MatButtonModule, RouterLink],
  selector: 'app-state-panel',
  styles: `
    .state-panel {
      display: grid;
      min-height: 18rem;
      place-items: center;
      align-content: center;
      gap: 0.5rem;
      text-align: center;
    }
    .state-icon {
      width: 3rem;
      height: 3rem;
      margin-bottom: 0.5rem;
    }
    p {
      max-width: 30rem;
      margin-bottom: 0.75rem;
    }
    .state-actions {
      display: flex;
      flex-wrap: wrap;
      justify-content: center;
      gap: 0.75rem;
    }
  `,
  template: `
    <section
      class="state-panel"
      [attr.role]="announce() ? 'alert' : null"
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
            ><app-icon [name]="backIcon()" />{{ backLabel() }}</a
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
  /** Announces the panel when it replaces content that failed to load; full error pages are already announced by the shell's focus move. */
  readonly announce = input(true);
  readonly retryable = input(false);
  readonly backLink = input<string | null>(null);
  readonly backLabel = input('');
  readonly backIcon = input<IconName>('chevron-left');
  readonly retry = output<void>();
}
