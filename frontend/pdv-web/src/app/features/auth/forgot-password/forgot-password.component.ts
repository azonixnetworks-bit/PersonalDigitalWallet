import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';
import { UiAlertComponent } from '../../../shared/components/ui-alert/ui-alert.component';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, UiAlertComponent],
  templateUrl: './forgot-password.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ForgotPasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);

  readonly loading = signal(false);
  readonly message = signal('');
  readonly messageTone = signal<'success' | 'error'>('success');

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]]
  });

  submit(): void {
    if (this.loading()) return;

    this.form.markAllAsTouched();
    if (this.form.invalid) {
      this.setMessage('Enter a valid email address.', 'error');
      return;
    }

    this.loading.set(true);
    this.message.set('');

    const email = this.form.controls.email.value.trim().toLowerCase();
    this.auth.forgotPassword({ email })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: result => this.setMessage(
          result.message || 'If the account is eligible, a password reset link will be sent.',
          'success'
        ),
        error: (error: Error) => this.setMessage(
          error.message || 'Password reset request could not be completed.',
          'error'
        )
      });
  }

  private setMessage(message: string, tone: 'success' | 'error'): void {
    this.messageTone.set(tone);
    this.message.set(message);
  }
}
