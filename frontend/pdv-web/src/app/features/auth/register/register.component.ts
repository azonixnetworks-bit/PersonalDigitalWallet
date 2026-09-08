import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthFlowStorageService } from '../../../core/auth/auth-flow-storage.service';
import { AuthService } from '../../../core/auth/auth.service';
import { UiAlertComponent } from '../../../shared/components/ui-alert/ui-alert.component';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, UiAlertComponent],
  templateUrl: './register.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly authFlow = inject(AuthFlowStorageService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly message = signal('');

  readonly form = this.fb.nonNullable.group({
    fullName: ['', [Validators.required, Validators.maxLength(150)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(128)]]
  });

  submit(): void {
    if (this.loading()) return;

    this.form.markAllAsTouched();
    if (this.form.invalid) {
      this.message.set('Check the highlighted fields and try again.');
      return;
    }

    this.loading.set(true);
    this.message.set('');

    const value = this.form.getRawValue();
    const email = value.email.trim().toLowerCase();

    this.auth.register({
      fullName: value.fullName.trim(),
      email,
      password: value.password
    })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: result => {
          this.authFlow.beginRegistration(result.email?.trim().toLowerCase() || email);
          void this.router.navigate(['/verify-email']);
        },
        error: (error: Error) => this.message.set(error.message || 'Registration could not be completed.')
      });
  }
}
