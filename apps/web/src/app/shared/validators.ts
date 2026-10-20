import { AbstractControl, ValidationErrors } from '@angular/forms';

/** Required text: rejects blank and whitespace-only values, which the API also rejects. */
export const requiredText = (
  control: AbstractControl,
): ValidationErrors | null =>
  String(control.value ?? '').trim() ? null : { required: true };
