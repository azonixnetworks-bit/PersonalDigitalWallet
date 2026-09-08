import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthFlowStorageService } from '../../../core/auth/auth-flow-storage.service';
import { AuthService } from '../../../core/auth/auth.service';
import { UiAlertComponent } from '../../../shared/components/ui-alert/ui-alert.component';

@Component({
  selector: 'app-verify-email',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, UiAlertComponent],
  templateUrl: './verify-email.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class VerifyEmailComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly authFlow = inject(AuthFlowStorageService);
  private readonly router = inject(Router);

  readonly email = signal(this.authFlow.registrationEmail());
  readonly verifying = signal(false);
  readonly resending = signal(false);
  readonly message = signal('');
  readonly messageTone = signal<'info' | 'success' | 'error'>('info');

  readonly form = this.fb.nonNullable.group({
    otp: ['', [Validators.required, Validators.pattern(/^\d{6}$/)]]
  });

  constructor() {
    if (!this.email()) {
      void this.router.navigate(['/register'], { replaceUrl: true });
    }
  }

  verify(): void {
    if (this.verifying() || !this.email()) return;

    this.form.markAllAsTouched();
    if (this.form.invalid) {
      this.setMessage('Enter the 6-digit verification code.', 'error');
      return;
    }

    this.verifying.set(true);
    this.message.set('');

    this.auth.verifyEmail({ email: this.email(), otp: this.form.controls.otp.value.trim() })
      .pipe(finalize(() => this.verifying.set(false)))
      .subscribe({
        next: result => {
          if (result.emailVerified !== true || !result.setupToken?.trim()) {
            this.setMessage('Email verification could not be completed.', 'error');
            return;
          }

          this.authFlow.setTotpSetupToken(result.setupToken);
          this.authFlow.clearTotpSetupResponse();
          this.form.reset();
          void this.router.navigate(['/setup-totp'], { replaceUrl: true });
        },
        error: (error: Error) => this.setMessage(
          error.message || 'The verification code is invalid or expired.',
          'error'
        )
      });
  }

  resend(): void {
    if (this.resending() || this.verifying() || !this.email()) return;

    this.resending.set(true);
    this.message.set('');

    this.auth.resendEmailOtp({ email: this.email() })
      .pipe(finalize(() => this.resending.set(false)))
      .subscribe({
        next: result => this.setMessage(
          result.message || 'If the account is eligible, a new verification code will be sent.',
          'success'
        ),
        error: (error: Error) => this.setMessage(
          error.message || 'The verification code could not be resent.',
          'error'
        )
      });
  }

  restart(): void {
    this.authFlow.clearRegistrationFlow();
    void this.router.navigate(['/register'], { replaceUrl: true });
  }

  private setMessage(message: string, tone: 'info' | 'success' | 'error'): void {
    this.messageTone.set(tone);
    this.message.set(message);
  }
}
