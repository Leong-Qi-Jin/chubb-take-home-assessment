import { Pipe, PipeTransform } from '@angular/core';

import { MoneyResponse } from '../core/api/generated';

// Always show the ISO code so mixed-currency APAC amounts are never ambiguous (en-SG would render SGD as a bare "$").
export function money(amount: string | number, currency: string): string {
  return new Intl.NumberFormat('en-SG', {
    style: 'currency',
    currency,
    currencyDisplay: 'code',
  }).format(Number(amount));
}

const currencyNames = new Intl.DisplayNames('en', { type: 'currency' });

export function currencyName(currency: string): string {
  return currencyNames.of(currency) ?? currency;
}

/** Formats an API money value with its ISO currency code, for example `SGD 5,850.00`. */
@Pipe({ name: 'money' })
export class MoneyPipe implements PipeTransform {
  transform(value: MoneyResponse | null | undefined): string {
    return value ? money(value.amount, value.currency) : '';
  }
}
