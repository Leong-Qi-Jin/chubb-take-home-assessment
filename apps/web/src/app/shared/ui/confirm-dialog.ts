import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import {
  MAT_DIALOG_DATA,
  MatDialog,
  MatDialogModule,
} from '@angular/material/dialog';
import { firstValueFrom } from 'rxjs';

import { Icon } from './icon';

export interface ConfirmOptions {
  title: string;
  message: string;
  confirmLabel: string;
  /** Destructive confirmations use the danger button. */
  destructive?: boolean;
}

/** Branded confirmation for final actions, such as settling or rejecting a claim. */
@Component({
  imports: [Icon, MatButtonModule, MatDialogModule],
  selector: 'app-confirm-dialog',
  template: `
    <h2 mat-dialog-title>{{ data.title }}</h2>
    <mat-dialog-content>
      <p>{{ data.message }}</p>
    </mat-dialog-content>
    <mat-dialog-actions>
      <button
        matButton="text"
        type="button"
        [mat-dialog-close]="false"
      >
        Cancel
      </button>
      <button
        type="button"
        [class.danger]="data.destructive"
        [mat-dialog-close]="true"
        [matButton]="data.destructive ? 'outlined' : 'filled'"
      >
        <app-icon [name]="data.destructive ? 'x' : 'check'" />{{
          data.confirmLabel
        }}
      </button>
    </mat-dialog-actions>
  `,
})
export class ConfirmDialog {
  protected readonly data = inject<ConfirmOptions>(MAT_DIALOG_DATA);
}

/** Opens the confirmation and resolves to true only when the user confirms. */
export async function confirmAction(
  dialog: MatDialog,
  options: ConfirmOptions,
): Promise<boolean> {
  const ref = dialog.open<ConfirmDialog, ConfirmOptions, boolean>(
    ConfirmDialog,
    { data: options, width: '28rem', autoFocus: 'dialog' },
  );
  return (await firstValueFrom(ref.afterClosed())) === true;
}
