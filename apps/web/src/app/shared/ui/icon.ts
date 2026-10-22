import { Component, computed, input } from '@angular/core';
import { NgIcon } from '@ng-icons/core';
import {
  lucideCalendar,
  lucideCar,
  lucideCheck,
  lucideChevronLeft,
  lucideChevronRight,
  lucideCircleAlert,
  lucideClock,
  lucideFileText,
  lucideHouse,
  lucideInbox,
  lucideLayoutDashboard,
  lucideList,
  lucideLock,
  lucideMessageSquare,
  lucidePencil,
  lucidePlus,
  lucideRotateCcw,
  lucideSearch,
  lucideSend,
  lucideUserCheck,
  lucideX,
} from '@ng-icons/lucide';

// The Lucide icons the app uses, under short names; only these are bundled.
const ICONS = {
  'alert-circle': lucideCircleAlert,
  calendar: lucideCalendar,
  car: lucideCar,
  check: lucideCheck,
  'chevron-left': lucideChevronLeft,
  'chevron-right': lucideChevronRight,
  clock: lucideClock,
  'file-text': lucideFileText,
  house: lucideHouse,
  inbox: lucideInbox,
  'layout-dashboard': lucideLayoutDashboard,
  list: lucideList,
  lock: lucideLock,
  'message-square': lucideMessageSquare,
  pencil: lucidePencil,
  plus: lucidePlus,
  'rotate-ccw': lucideRotateCcw,
  search: lucideSearch,
  send: lucideSend,
  'user-check': lucideUserCheck,
  x: lucideX,
} as const;

export type IconName = keyof typeof ICONS;

/** Decorative Lucide icon sized by `--icon-size` (`--icon-md` by default); pair it with visible text or an aria-label on the parent. */
@Component({
  imports: [NgIcon],
  selector: 'app-icon',
  host: { class: 'icon', 'aria-hidden': 'true' },
  styles: `
    :host {
      display: inline-flex;
      width: var(--icon-size, var(--icon-md));
      height: var(--icon-size, var(--icon-md));
      flex-shrink: 0;
    }
    ng-icon {
      display: block;
      width: 100%;
      height: 100%;
    }
  `,
  template: `<ng-icon
    size="100%"
    [svg]="svg()"
  />`,
})
export class Icon {
  readonly name = input.required<IconName>();
  protected readonly svg = computed(() => ICONS[this.name()]);
}
