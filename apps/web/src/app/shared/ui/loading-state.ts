import { Component, input } from '@angular/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

/** Centred page-level loading indicator; the message is announced politely to screen readers. */
@Component({
  imports: [MatProgressSpinnerModule],
  selector: 'app-loading-state',
  host: { class: 'loading-state', role: 'status' },
  styles: `
    :host {
      display: grid;
      min-height: 18rem;
      place-items: center;
      align-content: center;
      gap: 0.75rem;
      text-align: center;
    }
  `,
  template: `
    <mat-spinner
      diameter="32"
      strokeWidth="3"
      aria-hidden="true"
    />
    <p>{{ message() }}</p>
  `,
})
export class LoadingState {
  readonly message = input('Loading…');
}
