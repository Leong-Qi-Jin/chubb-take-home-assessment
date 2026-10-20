import { Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { RouterLink } from '@angular/router';

import { Icon } from '../shared/ui/icon';

@Component({
  imports: [Icon, MatButtonModule, RouterLink],
  selector: 'app-not-found',
  template: `
    <section class="state-panel">
      <span class="icon-tile state-icon"><app-icon name="search" /></span>
      <h1>Page not found</h1>
      <p>The page you requested does not exist.</p>
      <a
        matButton="filled"
        routerLink="/"
        ><app-icon name="house" />Return home</a
      >
    </section>
  `,
})
export class NotFound {}

@Component({
  imports: [Icon, MatButtonModule, RouterLink],
  selector: 'app-not-allowed',
  template: `
    <section class="state-panel">
      <span class="icon-tile state-icon"><app-icon name="lock" /></span>
      <h1>Not allowed</h1>
      <p>This page belongs to a different role. Switch users or return home.</p>
      <a
        matButton="filled"
        routerLink="/"
        ><app-icon name="house" />Return home</a
      >
    </section>
  `,
})
export class NotAllowed {}
