import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';
import { UiAlertComponent } from '../../../shared/components/ui-alert/ui-alert.component';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, UiAlertComponent],
  templateUrl: './reset-password.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ResetPasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  private resetToken = this.readAndRemoveResetTokenFromUrl();
  private redirectTimer: number | null = null;

  readonly loading = signal(false);
  readonly completed = signal(false);
  readonly blocked = signal(!this.resetToken);
  readonly message = signal(
    this.resetToken
      ? ''
      : 'This password reset link is missing or invalid. Request a new reset link.'
  );
  readonly messageTone = signal<'success' | 'error'>(this.resetToken ? 'success' : 'error');

  readonly form = this.fb.nonNullable.group({
    newPassword: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(128)]],
    confirmPassword: ['', Validators.required]
  });

  constructor() {
    this.destroyRef.onDestroy(() => {
      if (this.redirectTimer !== null) {
        window.clearTimeout(this.redirectTimer);
      }
    });
  }

  submit(): void {
    if (this.loading() || this.blocked() || this.completed()) return;

    this.form.markAllAsTouched();
    if (this.form.invalid) {
      this.setMessage('Enter and confirm a password containing at least 8 characters.', 'error');
      return;
    }

    const { newPassword, confirmPassword } = this.form.getRawValue();
    if (newPassword !== confirmPassword) {
      this.setMessage('Passwords do not match.', 'error');
      return;
    }

    if (!this.resetToken) {
      this.blocked.set(true);
      this.setMessage('This password reset link is invalid. Request a new reset link.', 'error');
      return;
    }

    this.loading.set(true);
    this.message.set('');

    this.auth.resetPassword({
      token: this.resetToken,
      newPassword,
      confirmPassword
    })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: result => {
          this.resetToken = '';
          this.completed.set(true);
          this.form.reset();
          this.form.disable({ emitEvent: false });
          this.setMessage(
            result.message || 'Password reset successfully. Please sign in using your new password.',
            'success'
          );

          this.redirectTimer = window.setTimeout(() => {
            void this.router.navigate(['/login'], {
              queryParams: { reset: 'success' },
              replaceUrl: true
            });
          }, 1800);
        },
        error: (error: Error) => this.setMessage(
          error.message || 'Password reset could not be completed.',
          'error'
        )
      });
  }

  passwordsMismatch(): boolean {
    const controls = this.form.controls;
    return controls.confirmPassword.touched
      && controls.confirmPassword.value.length > 0
      && controls.newPassword.value !== controls.confirmPassword.value;
  }

  private readAndRemoveResetTokenFromUrl(): string {
    const hash = window.location.hash;
    if (!hash || hash.length <= 1) return '';

    const params = new URLSearchParams(hash.substring(1));
    const token = params.get('token')?.trim() ?? '';

    if (token) {
      window.history.replaceState(
        null,
        '',
        `${window.location.pathname}${window.location.search}`
      );
    }

    return token;
  }

  private setMessage(message: string, tone: 'success' | 'error'): void {
    this.messageTone.set(tone);
    this.message.set(message);
  }
}
