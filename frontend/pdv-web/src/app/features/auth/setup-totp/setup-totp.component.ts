import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthFlowStorageService } from '../../../core/auth/auth-flow-storage.service';
import { AuthService } from '../../../core/auth/auth.service';
import { TotpSetupResponse } from '../../../core/models/auth.models';
import { UiAlertComponent } from '../../../shared/components/ui-alert/ui-alert.component';
import { UiLoaderComponent } from '../../../shared/components/ui-loader/ui-loader.component';

@Component({
  selector: 'app-setup-totp',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, UiAlertComponent, UiLoaderComponent],
  templateUrl: './setup-totp.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SetupTotpComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly authFlow = inject(AuthFlowStorageService);
  private readonly router = inject(Router);

  readonly setup = signal<TotpSetupResponse | null>(null);
  readonly loadingSetup = signal(false);
  readonly verifying = signal(false);
  readonly blocked = signal(false);
  readonly message = signal('');
  readonly messageTone = signal<'info' | 'success' | 'error'>('info');

  readonly form = this.fb.nonNullable.group({
    code: ['', [Validators.required, Validators.pattern(/^\d{6}$/)]]
  });

  ngOnInit(): void {
    const token = this.authFlow.totpSetupToken();
    if (!token) {
      this.block('Authenticator setup session is missing or expired. Please register again.');
      return;
    }

    const cached = this.authFlow.totpSetupResponse();
    if (cached) {
      this.setup.set(cached);
      return;
    }

    this.loadingSetup.set(true);
    this.auth.setupTotp({ setupToken: token })
      .pipe(finalize(() => this.loadingSetup.set(false)))
      .subscribe({
        next: result => {
          if (!result.secretKey?.trim() || !result.qrCodeDataUrl?.trim()) {
            this.block('Authenticator setup information could not be generated.');
            return;
          }

          this.authFlow.setTotpSetupResponse(result);
          this.setup.set(result);
        },
        error: (error: Error) => this.block(error.message || 'Authenticator setup could not be loaded.')
      });
  }

  verify(): void {
    if (this.verifying() || this.blocked()) return;

    const token = this.authFlow.totpSetupToken();
    if (!token) {
      this.block('Authenticator setup session has expired. Please register again.');
      return;
    }

    this.form.markAllAsTouched();
    if (this.form.invalid) {
      this.setMessage('Enter the 6-digit authenticator code.', 'error');
      return;
    }

    this.verifying.set(true);
    this.message.set('');

    this.auth.verifyTotpSetup({ setupToken: token, code: this.form.controls.code.value.trim() })
      .pipe(finalize(() => this.verifying.set(false)))
      .subscribe({
        next: () => {
          this.form.reset();
          this.authFlow.clearRegistrationFlow();
          void this.router.navigate(['/login'], {
            queryParams: { setup: 'complete' },
            replaceUrl: true
          });
        },
        error: (error: Error) => this.setMessage(
          error.message || 'The authenticator code is invalid or expired.',
          'error'
        )
      });
  }

  restart(): void {
    this.authFlow.clearRegistrationFlow();
    void this.router.navigate(['/register'], { replaceUrl: true });
  }

  private block(message: string): void {
    this.blocked.set(true);
    this.form.disable({ emitEvent: false });
    this.setMessage(message, 'error');
  }

  private setMessage(message: string, tone: 'info' | 'success' | 'error'): void {
    this.messageTone.set(tone);
    this.message.set(message);
  }
}
