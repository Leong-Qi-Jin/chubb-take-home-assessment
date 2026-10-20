import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { SubmitClaim } from './submit-claim';

describe('SubmitClaim', () => {
  function render(): HTMLElement {
    TestBed.configureTestingModule({
      imports: [SubmitClaim],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });
    const fixture = TestBed.createComponent(SubmitClaim);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('flags only the detail field for the chosen claim type', async () => {
    const element = render();
    const submit = element.querySelector<HTMLButtonElement>(
      'button[type="submit"]',
    )!;

    submit.click();
    await TestBed.inject(HttpTestingController)
      .match('/api/v1/currencies')
      .forEach((request) => request.flush(['SGD']));
    TestBed.tick();
    const errors = () =>
      Array.from(element.querySelectorAll('mat-error'), (error) =>
        error.textContent!.trim(),
      );
    expect(errors()).toContain('Enter the vehicle registration.');

    element
      .querySelectorAll<HTMLInputElement>('mat-radio-button input')[1]
      .click();
    submit.click();
    TestBed.tick();
    expect(errors()).toContain('Enter the property address.');
    expect(errors()).not.toContain('Enter the vehicle registration.');
    // Client validation stays on the fields instead of a page-level alert.
    expect(element.querySelector('.alert')).toBeNull();
  });

  it("rejects a time later than now on today's date", () => {
    TestBed.configureTestingModule({
      imports: [SubmitClaim],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });
    // The form is protected; reaching it directly keeps the test independent of picker interactions.
    const { form } = TestBed.createComponent(SubmitClaim)
      .componentInstance as unknown as { form: SubmitClaim['form'] };
    // A fixed midday clock keeps "an hour from now" on the same day.
    vi.useFakeTimers({ toFake: ['Date'] });
    vi.setSystemTime(new Date(2026, 9, 20, 12, 0));
    const now = new Date();
    const hour = 60 * 60 * 1000;
    form.controls.occurredDate.setValue(now);

    form.controls.occurredTime.setValue(new Date(now.getTime() + hour));
    expect(form.controls.occurredTime.hasError('future')).toBe(true);

    form.controls.occurredTime.setValue(new Date(now.getTime() - hour));
    expect(form.controls.occurredTime.valid).toBe(true);
    vi.useRealTimers();
  });
});
