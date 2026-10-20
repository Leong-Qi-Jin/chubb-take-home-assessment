import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

import { Icon } from './icon';

/** Page title block: optional back link, eyebrow, title with an optional inline badge (`[headingAside]`), description, and actions projected on the right. */
@Component({
  imports: [Icon, RouterLink],
  selector: 'app-page-header',
  host: { class: 'page-heading' },
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
