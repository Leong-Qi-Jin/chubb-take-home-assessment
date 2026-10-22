import { Component, input } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatInputModule } from '@angular/material/input';

/** Material amount field with the claim currency as a prefix and a decimal keypad on mobile. */
@Component({
  imports: [MatInputModule, ReactiveFormsModule],
  selector: 'app-money-field',
  styles: `
    mat-form-field {
      width: 100%;
    }
  `,
  template: `
    <mat-form-field
      floatLabel="always"
      subscriptSizing="dynamic"
    >
      <mat-label>{{ label() }}</mat-label>
      <span matTextPrefix>{{ currency() }}&nbsp;</span>
      <input
        matInput
        type="text"
        inputmode="decimal"
        [formControl]="control()"
      />
    </mat-form-field>
  `,
})
export class MoneyField {
  readonly label = input.required<string>();
  readonly currency = input.required<string>();
  readonly control = input.required<FormControl<string>>();
}
