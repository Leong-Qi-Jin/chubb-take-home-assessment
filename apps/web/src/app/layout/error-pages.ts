import { Component } from '@angular/core';

import { StatePanel } from '../shared/ui/state-panel';

@Component({
  imports: [StatePanel],
  selector: 'app-not-found',
  template: `
    <app-state-panel
      icon="search"
      title="Page not found"
      message="The page you requested does not exist."
      backLink="/"
      backLabel="Return home"
      backIcon="house"
      [announce]="false"
    />
  `,
})
export class NotFound {}

@Component({
  imports: [StatePanel],
  selector: 'app-not-allowed',
  template: `
    <app-state-panel
      icon="lock"
      title="Not allowed"
      message="This page belongs to a different role. Switch users or return home."
      backLink="/"
      backLabel="Return home"
      backIcon="house"
      [announce]="false"
    />
  `,
})
export class NotAllowed {}
