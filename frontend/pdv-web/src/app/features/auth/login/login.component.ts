import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthFlowStorageService } from '../../../core/auth/auth-flow-storage.service';
import { AuthService } from '../../../core/auth/auth.service';
import { TokenService } from '../../../core/auth/token.service';
import { UiAlertComponent } from '../../../shared/components/ui-alert/ui-alert.component';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, UiAlertComponent],
  templateUrl: './login.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly authFlow = inject(AuthFlowStorageService);
  private readonly tokens = inject(TokenService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly loading = signal(false);
  readonly message = signal(this.initialMessage());
  readonly messageTone = signal<'info' | 'success' | 'error'>('info');

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required]
  });

  constructor() {
    const params = this.route.snapshot.queryParamMap;
    if (params.get('setup') === 'complete' || params.get('reset') === 'success') {
      this.messageTone.set('success');
    }
  }

  submit(): void {
    if (this.loading()) return;

    this.form.markAllAsTouched();
    if (this.form.invalid) {
      this.setError('Enter a valid email address and password.');
      return;
    }

    this.loading.set(true);
    this.message.set('');

    const { email, password } = this.form.getRawValue();
    const normalizedEmail = email.trim().toLowerCase();

    this.auth.login({ email: normalizedEmail, password })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: result => {
          if (result.requiresTotp === true) {
            if (!result.challengeToken?.trim()) {
              this.setError('The login security challenge could not be created.');
              return;
            }

            this.tokens.clear();
            this.authFlow.setLoginChallenge(
              result.challengeToken,
              result.email?.trim() || normalizedEmail
            );

            void this.router.navigate(['/verify-login-otp']);
            return;
          }

          if (result.requiresTotp === false && result.token?.trim()) {
            this.tokens.set(result.token);
            this.authFlow.clearLoginChallenge();

            const destination = String(result.role).toLowerCase() === 'admin'
              ? '/admin'
              : this.authFlow.consumePostLoginRedirect();

            void this.router.navigateByUrl(destination, { replaceUrl: true });
            return;
          }

          this.setError('Login could not be completed.');
        },
        error: (error: Error) => this.setError(error.message || 'Login failed.')
      });
  }

  private initialMessage(): string {
    const params = this.route.snapshot.queryParamMap;

    if (params.get('setup') === 'complete') {
      return 'Two-factor authentication is enabled. Sign in to continue.';
    }
    if (params.get('reset') === 'success') {
      return 'Password reset successfully. Sign in using your new password.';
    }

    const reason = params.get('reason');
    if (reason === 'expired') return 'Your session expired. Please sign in again.';
    if (reason === 'unauthorized') return 'Please sign in to continue.';
    if (reason === 'share') return 'Sign in with the account that received the secure document invitation.';
    if (reason === 'mfa-missing') return 'Your login verification session is missing or expired. Please sign in again.';
    return '';
  }

  private setError(message: string): void {
    this.messageTone.set('error');
    this.message.set(message);
  }
}
