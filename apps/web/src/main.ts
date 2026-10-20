import { bootstrapApplication } from '@angular/platform-browser';

import { App } from './app/app';
import { appConfig } from './app/app.config';

// A failed bootstrap leaves no UI to report the error, so the console is the only place it can surface.
// eslint-disable-next-line no-console
bootstrapApplication(App, appConfig).catch((err) => console.error(err));
