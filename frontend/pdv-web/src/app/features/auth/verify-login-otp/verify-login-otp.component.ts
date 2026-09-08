import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthFlowStorageService } from '../../../core/auth/auth-flow-storage.service';
import { AuthService } from '../../../core/auth/auth.service';
import { TokenService } from '../../../core/auth/token.service';
import { UiAlertComponent } from '../../../shared/components/ui-alert/ui-alert.component';

@Component({
  selector: 'app-verify-login-otp',
  standalone: true,
  imports: [ReactiveFormsModule, UiAlertComponent],
  templateUrl: './verify-login-otp.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class VerifyLoginOtpComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly authFlow = inject(AuthFlowStorageService);
  private readonly tokens = inject(TokenService);
  private readonly router = inject(Router);

  readonly email = signal(this.authFlow.loginEmail());
  readonly loading = signal(false);
  readonly message = signal('');

  readonly form = this.fb.nonNullable.group({
    code: ['', [Validators.required, Validators.pattern(/^\d{6}$/)]]
  });

  constructor() {
    if (!this.authFlow.loginChallenge()) {
      void this.router.navigate(['/login'], {
        queryParams: { reason: 'mfa-missing' },
        replaceUrl: true
      });
    }
  }

  verify(): void {
    if (this.loading()) return;

    const challengeToken = this.authFlow.loginChallenge();
    if (!challengeToken) {
      this.message.set('Your login verification session has expired.');
      return;
    }

    this.form.markAllAsTouched();
    if (this.form.invalid) {
      this.message.set('Enter the 6-digit authenticator code.');
      return;
    }

    this.loading.set(true);
    this.message.set('');

    this.auth.verifyLoginTotp({
      challengeToken,
      code: this.form.controls.code.value.trim()
    })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: result => {
          if (!result.token?.trim()) {
            this.message.set('Login verification could not be completed.');
            return;
          }

          this.tokens.set(result.token);
          this.authFlow.clearLoginChallenge();
          this.form.reset();

          const destination = String(result.role).toLowerCase() === 'admin'
            ? '/admin'
            : this.authFlow.consumePostLoginRedirect();

          void this.router.navigateByUrl(destination, { replaceUrl: true });
        },
        error: (error: Error) => this.message.set(
          error.message || 'The authenticator code is invalid or expired.'
        )
      });
  }

  cancel(): void {
    this.authFlow.clearLoginChallenge();
    void this.router.navigate(['/login'], { replaceUrl: true });
  }
}
