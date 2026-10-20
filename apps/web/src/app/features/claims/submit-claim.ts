import { TextFieldModule } from '@angular/cdk/text-field';
import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DATE_LOCALE } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatInputModule } from '@angular/material/input';
import { MatRadioModule } from '@angular/material/radio';
import { MatSelectModule } from '@angular/material/select';
import { MatTimepickerModule } from '@angular/material/timepicker';
import { provideDateFnsAdapter } from '@angular/material-date-fns-adapter';
import { Router, RouterLink } from '@angular/router';
import { enGB } from 'date-fns/locale';
import { startWith } from 'rxjs';

import { ClaimsApi } from '../../core/api/claims-api';
import { ClaimType, SubmitClaimRequest } from '../../core/api/generated';
import { ROLE_ROUTES } from '../../core/identity/identity';
import { Alert } from '../../shared/ui/alert';
import { Icon } from '../../shared/ui/icon';
import { PageHeader } from '../../shared/ui/page-header';
import { SectionHeader } from '../../shared/ui/section-header';
import { requiredText } from '../../shared/validators';
import { CLAIM_TYPES } from './claim-type';

// Day-first dates and 12-hour times match how APAC claimants write them and how the rest of the app displays them. Typing accepts single-digit days and months, and 24-hour times.
// Provided on this page only so the date library stays out of the initial bundle.
const CLAIM_DATE_FORMATS = {
  parse: {
    dateInput: ['dd/MM/yyyy', 'd/M/yyyy'],
    timeInput: ['h:mm a', 'H:mm'],
  },
  display: {
    dateInput: 'dd/MM/yyyy',
    timeInput: 'h:mm a',
    monthYearLabel: 'LLL yyyy',
    dateA11yLabel: 'd MMMM yyyy',
    monthYearA11yLabel: 'LLLL yyyy',
    timeOptionLabel: 'h:mm a',
  },
};

// Builds the incident instant from the separate date and time pickers, using the date's day and the time's hours and minutes.
function combineDateAndTime(date: Date, time: Date): Date {
  return new Date(
    date.getFullYear(),
    date.getMonth(),
    date.getDate(),
    time.getHours(),
    time.getMinutes(),
  );
}

// The date picker blocks future days; this catches a later time on today's date.
const notInFuture = (time: AbstractControl): ValidationErrors | null => {
  const date = time.parent?.get('occurredDate')?.value as Date | null;
  if (!date || !(time.value instanceof Date)) return null;
  return combineDateAndTime(date, time.value) > new Date()
    ? { future: true }
    : null;
};

@Component({
  imports: [
    MatButtonModule,
    MatDatepickerModule,
    MatInputModule,
    MatTimepickerModule,
    TextFieldModule,
    MatRadioModule,
    MatSelectModule,
    ReactiveFormsModule,
    RouterLink,
    Alert,
    Icon,
    PageHeader,
    SectionHeader,
  ],
  providers: [
    provideDateFnsAdapter(CLAIM_DATE_FORMATS),
    { provide: MAT_DATE_LOCALE, useValue: enGB },
  ],
  selector: 'app-submit-claim',
  template: `
    <app-page-header
      eyebrow="Report an incident"
      heading="New claim"
      description="Enter the incident details. You can follow the claim’s status after you submit it."
      backLink="/my-claims"
      backLabel="My claims"
    />
    <form
      class="form-layout"
      [formGroup]="form"
      (ngSubmit)="submit()"
    >
      <section class="surface form-section">
        <div class="step-number">1</div>
        <app-section-header
          heading="Claim type"
          description="Choose the type of claim."
        />
        <mat-radio-group
          formControlName="type"
          class="choice-cards"
        >
          <mat-radio-button value="Motor"
            ><strong class="choice-title">Motor</strong
            ><span class="choice-description"
              >Vehicle damage or road incidents</span
            ></mat-radio-button
          >
          <mat-radio-button value="Property"
            ><strong class="choice-title">Property</strong
            ><span class="choice-description"
              >Damage to a home or building</span
            ></mat-radio-button
          >
        </mat-radio-group>
      </section>
      <section class="surface form-section">
        <div class="step-number">2</div>
        <app-section-header
          heading="Policy and incident"
          description="Enter your policy number and the incident details."
        />
        <div class="field-grid">
          <mat-form-field class="full"
            ><mat-label>Policy number</mat-label
            ><input
              matInput
              formControlName="policyNumber"
              maxlength="50"
              placeholder="POL-123456"
            /><mat-error
              >A policy number is required.</mat-error
            ></mat-form-field
          >
          <mat-form-field>
            <mat-label>Incident date</mat-label>
            <input
              matInput
              formControlName="occurredDate"
              placeholder="dd/mm/yyyy"
              [matDatepicker]="datePicker"
              [max]="today"
            />
            <mat-datepicker-toggle
              matIconSuffix
              [for]="datePicker"
              ><app-icon
                matDatepickerToggleIcon
                name="calendar"
            /></mat-datepicker-toggle>
            <mat-datepicker #datePicker />
            <mat-error>
              @if (form.controls.occurredDate.hasError('matDatepickerParse')) {
                Enter the date as dd/mm/yyyy.
              } @else if (
                form.controls.occurredDate.hasError('matDatepickerMax')
              ) {
                The incident date cannot be in the future.
              } @else {
                Choose the date of the incident.
              }
            </mat-error>
          </mat-form-field>
          <mat-form-field>
            <mat-label>Incident time</mat-label>
            <input
              matInput
              formControlName="occurredTime"
              placeholder="h:mm AM"
              [matTimepicker]="timePicker"
            />
            <mat-timepicker-toggle
              matIconSuffix
              [for]="timePicker"
              ><app-icon
                matTimepickerToggleIcon
                name="clock"
            /></mat-timepicker-toggle>
            <mat-timepicker #timePicker />
            <mat-error>
              @if (form.controls.occurredTime.hasError('matTimepickerParse')) {
                Enter a time such as 4:30 PM.
              } @else if (form.controls.occurredTime.hasError('future')) {
                The incident time cannot be in the future.
              } @else {
                Choose the time of the incident.
              }
            </mat-error>
          </mat-form-field>
          <mat-form-field class="full"
            ><mat-label>Incident location</mat-label
            ><input
              matInput
              formControlName="location"
              maxlength="500"
            /><mat-error
              >Enter the incident location.</mat-error
            ></mat-form-field
          >
          <mat-form-field class="full"
            ><mat-label>Incident description</mat-label
            ><textarea
              matInput
              cdkTextareaAutosize
              cdkAutosizeMinRows="5"
              formControlName="description"
              maxlength="2000"
              placeholder="Describe how the incident happened and the damage caused"
            ></textarea
            ><mat-error
              >Enter a description of the incident.</mat-error
            ></mat-form-field
          >
        </div>
      </section>
      <section class="surface form-section">
        <div class="step-number">3</div>
        <app-section-header
          [heading]="
            form.controls.type.value === 'Motor'
              ? 'Vehicle details'
              : 'Property details'
          "
          [description]="
            form.controls.type.value === 'Motor'
              ? 'Enter the vehicle registration and the claim currency.'
              : 'Enter the property address and the claim currency.'
          "
        />
        <div class="field-grid">
          @if (form.controls.type.value === 'Motor') {
            <mat-form-field class="full"
              ><mat-label>{{ claimTypes.Motor.detailLabel }}</mat-label
              ><input
                matInput
                formControlName="vehicleRegistration"
                maxlength="50"
                placeholder="SBA1234A"
              /><mat-error
                >Enter the vehicle registration.</mat-error
              ></mat-form-field
            >
          } @else {
            <mat-form-field class="full"
              ><mat-label>{{ claimTypes.Property.detailLabel }}</mat-label
              ><textarea
                matInput
                cdkTextareaAutosize
                cdkAutosizeMinRows="3"
                formControlName="propertyAddress"
                maxlength="500"
              ></textarea
              ><mat-error>Enter the property address.</mat-error>
            </mat-form-field>
          }
          <mat-form-field
            ><mat-label>Claim currency</mat-label
            ><mat-select formControlName="currency">
              @for (currency of currencies(); track currency) {
                <mat-option [value]="currency">{{ currency }}</mat-option>
              }
            </mat-select></mat-form-field
          >
        </div>
      </section>
      @if (currencyError()) {
        <app-alert tone="danger"
          >{{ currencyError() }}
          <button
            alertAction
            matButton="text"
            type="button"
            (click)="loadCurrencies()"
          >
            <app-icon name="rotate-ccw" />Try again
          </button>
        </app-alert>
      }
      @if (error()) {
        <app-alert tone="danger">{{ error() }}</app-alert>
      }
      <div class="form-actions">
        <a
          matButton="text"
          routerLink="/my-claims"
          >Cancel</a
        ><button
          matButton="filled"
          type="submit"
          [disabled]="submitting()"
        >
          <app-icon name="send" />{{
            submitting() ? 'Submitting…' : 'Submit claim'
          }}
        </button>
      </div>
    </form>
  `,
})
export class SubmitClaim {
  private readonly api = inject(ClaimsApi);
  private readonly router = inject(Router);
  protected readonly currencies = signal<string[]>([]);
  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly currencyError = signal<string | null>(null);
  protected readonly today = new Date();
  protected readonly claimTypes = CLAIM_TYPES;
  protected readonly form = new FormGroup({
    type: new FormControl<ClaimType>('Motor', { nonNullable: true }),
    policyNumber: new FormControl('', {
      nonNullable: true,
      validators: [requiredText, Validators.maxLength(50)],
    }),
    occurredDate: new FormControl<Date | null>(null, {
      validators: [Validators.required],
    }),
    occurredTime: new FormControl<Date | null>(null, {
      validators: [Validators.required, notInFuture],
    }),
    location: new FormControl('', {
      nonNullable: true,
      validators: [requiredText, Validators.maxLength(500)],
    }),
    description: new FormControl('', {
      nonNullable: true,
      validators: [requiredText, Validators.maxLength(2000)],
    }),
    vehicleRegistration: new FormControl('', { nonNullable: true }),
    propertyAddress: new FormControl('', { nonNullable: true }),
    currency: new FormControl('SGD', {
      nonNullable: true,
      validators: [Validators.required],
    }),
  });
  constructor() {
    void this.loadCurrencies();
    // The time's "not in the future" rule depends on the chosen date.
    this.form.controls.occurredDate.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(() =>
        this.form.controls.occurredTime.updateValueAndValidity(),
      );
    // Only the detail field for the chosen claim type is required, so its error shows on the field itself.
    this.form.controls.type.valueChanges
      .pipe(startWith(this.form.controls.type.value), takeUntilDestroyed())
      .subscribe((type) => {
        const { vehicleRegistration, propertyAddress } = this.form.controls;
        const [required, optional] =
          type === 'Motor'
            ? [vehicleRegistration, propertyAddress]
            : [propertyAddress, vehicleRegistration];
        required.setValidators(requiredText);
        optional.clearValidators();
        required.updateValueAndValidity({ emitEvent: false });
        optional.updateValueAndValidity({ emitEvent: false });
      });
  }
  protected async loadCurrencies(): Promise<void> {
    this.currencyError.set(null);
    try {
      const values = await this.api.currencies();
      this.currencies.set(values);
      if (values.length && !values.includes(this.form.controls.currency.value))
        this.form.controls.currency.setValue(values[0]);
    } catch (error) {
      this.currencyError.set(
        this.api.problemMessage(error, 'Currencies could not be loaded.'),
      );
    }
  }
  protected async submit(): Promise<void> {
    const raw = this.form.getRawValue();
    if (this.form.invalid) {
      // Field errors explain what is missing; the alert is reserved for server and loading errors.
      this.form.markAllAsTouched();
      this.error.set(null);
      return;
    }
    this.submitting.set(true);
    this.error.set(null);
    try {
      const request: SubmitClaimRequest = {
        type: raw.type,
        policyNumber: raw.policyNumber.trim(),
        incident: {
          occurredAt: combineDateAndTime(
            raw.occurredDate!,
            raw.occurredTime!,
          ).toISOString(),
          location: raw.location.trim(),
          description: raw.description.trim(),
        },
        motor:
          raw.type === 'Motor'
            ? { vehicleRegistration: raw.vehicleRegistration.trim() }
            : null,
        property:
          raw.type === 'Property'
            ? { propertyAddress: raw.propertyAddress.trim() }
            : null,
        currency: raw.currency,
      };
      const result = await this.api.submit(request);
      await this.router.navigate(
        [ROLE_ROUTES.claimant.claims, result.claim.reference],
        {
          state: { justSubmitted: true },
        },
      );
    } catch (error) {
      const problem = this.api.problem(error);
      this.error.set(
        problem.detail ??
          (Object.values(problem.errors ?? {})
            .flat()
            .join(' ') ||
            'The claim could not be submitted.'),
      );
    } finally {
      this.submitting.set(false);
    }
  }
}
