import { Component, input } from '@angular/core';

import { Tone } from './tone';

/** Status badge. Every badge in the app uses one of the shared tones so colours stay consistent with alerts. */
@Component({
  selector: 'app-badge',
  host: { class: 'badge', '[attr.data-tone]': 'tone()' },
  template: `<ng-content />`,
})
export class Badge {
  readonly tone = input<Tone>('neutral');
}
