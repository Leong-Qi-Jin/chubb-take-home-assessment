import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Title } from '@angular/platform-browser';
import { provideRouter, TitleStrategy } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';

import { AppTitleStrategy } from './title-strategy';

@Component({ template: '' })
class Blank {}

describe('AppTitleStrategy', () => {
  it('appends the app name to the route title', async () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          { path: 'queue', title: 'Work queue', component: Blank },
        ]),
        { provide: TitleStrategy, useClass: AppTitleStrategy },
      ],
    });
    const harness = await RouterTestingHarness.create();
    const title = TestBed.inject(Title);

    await harness.navigateByUrl('/queue');
    expect(title.getTitle()).toBe('Work queue · ClaimTrack');
  });
});
