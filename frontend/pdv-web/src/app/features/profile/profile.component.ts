import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize, take } from 'rxjs';
import { ProfileDto } from '../../core/models/profile.models';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { UiLoaderComponent } from '../../shared/components/ui-loader/ui-loader.component';
import { ProfileService } from './profile.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent, UiLoaderComponent],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProfileComponent {
  private readonly profileService = inject(ProfileService);

  readonly profile = signal<ProfileDto | null>(null);
  readonly loading = signal(true);
  readonly refreshing = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly success = signal('');

  readonly form = new FormGroup({
    fullName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(200)]
    }),
    email: new FormControl({ value: '', disabled: true }, { nonNullable: true })
  });

  readonly initials = computed(() => {
    const name = this.profile()?.fullName?.trim();
    if (!name) return 'PD';
    return name
      .split(/\s+/)
      .slice(0, 2)
      .map(part => part.charAt(0).toUpperCase())
      .join('');
  });

  readonly securityComplete = computed(() => {
    const profile = this.profile();
    return !!profile?.isActive && !!profile?.isEmailVerified && !!profile?.isTotpEnabled;
  });

  readonly securitySummary = computed(() => {
    const profile = this.profile();
    if (!profile) return '';
    const enabled = [profile.isActive, profile.isEmailVerified, profile.isTotpEnabled]
      .filter(Boolean).length;
    return `${enabled}/3 protections active`;
  });

  constructor() {
    this.load();
  }

  refresh(): void {
    if (this.loading() || this.refreshing() || this.saving()) return;
    this.load(true);
  }

  save(): void {
    this.error.set('');
    this.success.set('');

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const fullName = this.form.controls.fullName.value.trim();
    if (!fullName) {
      this.form.controls.fullName.setErrors({ required: true });
      this.form.controls.fullName.markAsTouched();
      return;
    }

    this.saving.set(true);
    this.profileService.updateProfile({ fullName }).pipe(
      take(1),
      finalize(() => this.saving.set(false))
    ).subscribe({
      next: profile => {
        this.applyProfile(profile);
        this.success.set('Profile updated successfully.');
      },
      error: error => this.error.set(this.apiMessage(error, 'Profile could not be updated.'))
    });
  }

  formatDate(value: string | null | undefined): string {
    if (!value) return '—';
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return '—';
    return new Intl.DateTimeFormat(undefined, {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    }).format(date);
  }

  private load(isRefresh = false): void {
    this.error.set('');
    this.success.set('');
    if (isRefresh) this.refreshing.set(true);
    else this.loading.set(true);

    this.profileService.getProfile().pipe(
      take(1),
      finalize(() => {
        this.loading.set(false);
        this.refreshing.set(false);
      })
    ).subscribe({
      next: profile => this.applyProfile(profile),
      error: error => this.error.set(this.apiMessage(error, 'Profile could not be loaded.'))
    });
  }

  private applyProfile(profile: ProfileDto): void {
    this.profile.set(profile);
    this.form.patchValue({
      fullName: profile.fullName || '',
      email: profile.email || ''
    }, { emitEvent: false });
    this.form.markAsPristine();
  }

  private apiMessage(error: unknown, fallback: string): string {
    if (error instanceof HttpErrorResponse) {
      const body = error.error as { message?: unknown; title?: unknown } | null;
      if (typeof body?.message === 'string' && body.message.trim()) return body.message;
      if (typeof body?.title === 'string' && body.title.trim()) return body.title;
    }
    return error instanceof Error && error.message ? error.message : fallback;
  }
}
