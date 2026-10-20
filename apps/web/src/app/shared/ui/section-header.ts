import { Component, input } from '@angular/core';

/** Heading for a card section: optional eyebrow, title, optional description, and projected content on the right such as an icon. */
@Component({
  selector: 'app-section-header',
  host: { class: 'section-heading' },
  template: `
    <div>
      @if (eyebrow()) {
        <span class="eyebrow">{{ eyebrow() }}</span>
      }
      <h2>{{ heading() }}</h2>
      @if (description()) {
        <p>{{ description() }}</p>
      }
    </div>
    <ng-content />
  `,
})
export class SectionHeader {
  readonly heading = input.required<string>();
  readonly eyebrow = input('');
  readonly description = input('');
}
