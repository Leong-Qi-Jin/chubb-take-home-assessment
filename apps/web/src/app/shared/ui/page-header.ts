import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

import { Icon } from './icon';

/** Page title block: optional back link, eyebrow, title with an optional inline badge (`[headingAside]`), description, and actions projected on the right. */
@Component({
  imports: [Icon, RouterLink],
  selector: 'app-page-header',
  host: { class: 'page-heading' },
  styles: `
    :host {
      display: flex;
      align-items: end;
      justify-content: space-between;
      gap: 1.5rem;
      margin-bottom: 2rem;
    }
    p {
      margin-top: 0.5rem;
      font-size: var(--text-md);
    }
    .back-link {
      display: inline-flex;
      align-items: center;
      gap: 0.25rem;
      width: max-content;
      min-height: 2rem;
      margin-bottom: 0.5rem;
      color: var(--navy);
      font-size: var(--text-sm);
      font-weight: 700;
      text-decoration: none;
    }
    .back-link app-icon {
      --icon-size: var(--icon-sm);
    }
    .back-link:hover {
      text-decoration: underline;
    }
    /* The badge sits on the title's first-line baseline, so it stays with the first line when a long title wraps. */
    .title-line {
      display: flex;
      align-items: baseline;
      flex-wrap: wrap;
      gap: 0.75rem;
    }
    @media (max-width: 52rem) {
      :host {
        align-items: start;
        flex-direction: column;
        gap: 1rem;
      }
    }
  `,
  template: `
    <div>
      @if (backLink(); as link) {
        <a
          class="back-link"
          [routerLink]="link"
          ><app-icon name="chevron-left" />{{ backLabel() }}</a
        >
      }
      @if (eyebrow()) {
        <span class="eyebrow">{{ eyebrow() }}</span>
      }
      <div class="title-line">
        <h1>{{ heading() }}</h1>
        <ng-content select="[headingAside]" />
      </div>
      @if (description()) {
        <p>{{ description() }}</p>
      }
    </div>
    <ng-content />
  `,
})
export class PageHeader {
  readonly heading = input.required<string>();
  readonly eyebrow = input('');
  readonly description = input('');
  readonly backLink = input<string | null>(null);
  readonly backLabel = input('');
}
