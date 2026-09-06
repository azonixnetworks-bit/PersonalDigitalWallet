import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, computed, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import {
  CreateCredentialRequest,
  CredentialDto
} from '../../core/models/vault.models';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { UiEmptyStateComponent } from '../../shared/components/ui-empty-state/ui-empty-state.component';
import { UiLoaderComponent } from '../../shared/components/ui-loader/ui-loader.component';
import { NotificationService } from '../../shared/services/notification.service';
import { CredentialService } from './credential.service';

@Component({
  selector: 'app-credentials',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent, UiEmptyStateComponent, UiLoaderComponent],
  templateUrl: './credentials.component.html',
  styleUrl: './credentials.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CredentialsComponent implements OnInit, OnDestroy {
  readonly credentials = signal<CredentialDto[]>([]);
  readonly loading = signal(true);
  readonly refreshing = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly searchTerm = signal('');
  readonly busyCredentialId = signal<number | null>(null);
  readonly editingCredentialId = signal<number | null>(null);
  readonly revealedCredential = signal<CredentialDto | null>(null);
  readonly revealBusyId = signal<number | null>(null);
  readonly passwordVisible = signal(false);
  readonly revealExpiresIn = signal<number | null>(null);

  private revealTimer: number | null = null;
  private revealCountdownTimer: number | null = null;

  readonly form = new FormGroup({
    title: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(250)] }),
    username: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(500)] }),
    password: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(1000)] }),
    website: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(2000)] }),
    notes: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(5000)] })
  });

  readonly filteredCredentials = computed(() => {
    const query = this.searchTerm().trim().toLocaleLowerCase();
    if (!query) return this.credentials();

    return this.credentials().filter(item => {
      const searchable = [item.title, item.username, item.website ?? '']
        .join(' ')
        .toLocaleLowerCase();
      return searchable.includes(query);
    });
  });

  readonly credentialCountLabel = computed(() => {
    const visible = this.filteredCredentials().length;
    const total = this.credentials().length;
    if (this.searchTerm().trim()) return `${visible} of ${total} credentials`;
    return `${total} ${total === 1 ? 'credential' : 'credentials'}`;
  });

  readonly editing = computed(() => this.editingCredentialId() !== null);

  constructor(
    private readonly credentialsApi: CredentialService,
    private readonly notifications: NotificationService
  ) {}

  ngOnInit(): void {
    this.loadCredentials();
  }

  ngOnDestroy(): void {
    this.clearSensitiveState();
  }

  loadCredentials(refresh = false): void {
    this.error.set('');
    refresh ? this.refreshing.set(true) : this.loading.set(true);

    this.credentialsApi.list().pipe(
      finalize(() => {
        this.loading.set(false);
        this.refreshing.set(false);
      })
    ).subscribe({
      next: credentials => this.credentials.set(Array.isArray(credentials) ? credentials : []),
      error: error => this.error.set(this.message(error, 'Credentials could not be loaded.'))
    });
  }

  setSearchTerm(value: string): void {
    this.searchTerm.set(value);
  }

  saveCredential(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.saving()) return;

    const request = this.buildRequest();
    if (!request) return;

    this.saving.set(true);
    const id = this.editingCredentialId();
    const operation = id === null
      ? this.credentialsApi.create(request)
      : this.credentialsApi.update(id, request);

    operation.pipe(finalize(() => this.saving.set(false))).subscribe({
      next: result => {
        if (id === null) {
          this.credentials.update(items => [result, ...items.filter(item => item.id !== result.id)]);
          this.notifications.success('Credential saved securely.');
        } else {
          this.credentials.update(items => items.map(item => item.id === result.id ? result : item));
          this.notifications.success('Credential updated securely.');
        }
        this.resetForm();
        this.hideRevealed();
      },
      error: error => this.notifications.error(this.message(error, 'Credential could not be saved.'))
    });
  }

  revealCredential(credential: CredentialDto): void {
    if (!this.validId(credential.id) || this.revealBusyId() !== null) return;

    this.revealBusyId.set(credential.id);
    this.hideRevealed();
    this.credentialsApi.get(credential.id).pipe(
      finalize(() => this.revealBusyId.set(null))
    ).subscribe({
      next: revealed => {
        this.revealedCredential.set(revealed);
        this.startRevealExpiry();
      },
      error: error => this.notifications.error(this.message(error, 'Credential could not be revealed.'))
    });
  }

  editCredential(credential: CredentialDto): void {
    if (!this.validId(credential.id) || this.busyCredentialId() !== null) return;

    this.busyCredentialId.set(credential.id);
    this.credentialsApi.get(credential.id).pipe(
      finalize(() => this.busyCredentialId.set(null))
    ).subscribe({
      next: revealed => {
        this.editingCredentialId.set(credential.id);
        this.form.setValue({
          title: revealed.title ?? '',
          username: revealed.username ?? '',
          password: revealed.password ?? '',
          website: revealed.website ?? '',
          notes: revealed.notes ?? ''
        });
        this.passwordVisible.set(false);
        this.hideRevealed();
        window.requestAnimationFrame(() => document.getElementById('credential-title')?.focus());
      },
      error: error => this.notifications.error(this.message(error, 'Credential could not be loaded for editing.'))
    });
  }

  cancelEdit(): void {
    this.resetForm();
    this.notifications.show('Edit cancelled.', 'info', 2600);
  }

  deleteCredential(credential: CredentialDto): void {
    if (!this.validId(credential.id) || this.busyCredentialId() !== null) return;

    const confirmed = window.confirm(`Delete "${credential.title || 'this credential'}" from your vault? This cannot be undone.`);
    if (!confirmed) return;

    this.busyCredentialId.set(credential.id);
    this.credentialsApi.remove(credential.id).pipe(
      finalize(() => this.busyCredentialId.set(null))
    ).subscribe({
      next: () => {
        this.credentials.update(items => items.filter(item => item.id !== credential.id));
        if (this.editingCredentialId() === credential.id) this.resetForm();
        if (this.revealedCredential()?.id === credential.id) this.hideRevealed();
        this.notifications.success('Credential deleted successfully.');
      },
      error: error => this.notifications.error(this.message(error, 'Credential could not be deleted.'))
    });
  }

  togglePasswordVisibility(): void {
    this.passwordVisible.update(value => !value);
  }

  hideRevealed(): void {
    this.clearRevealTimers();
    this.revealExpiresIn.set(null);
    this.revealedCredential.set(null);
  }

  async copyUsername(credential: CredentialDto): Promise<void> {
    await this.copyText(credential.username, 'Username copied.');
  }

  async copyRevealedUsername(): Promise<void> {
    const revealed = this.revealedCredential();
    if (!revealed) return;
    await this.copyText(revealed.username, 'Username copied.');
  }

  async copyRevealedPassword(): Promise<void> {
    const revealed = this.revealedCredential();
    if (!revealed) return;
    await this.copyText(revealed.password, 'Password copied.');
  }

  safeWebsite(value: string | null | undefined): string | null {
    const input = value?.trim();
    if (!input) return null;
    try {
      const url = new URL(input);
      return url.protocol === 'http:' || url.protocol === 'https:' ? url.toString() : null;
    } catch {
      return null;
    }
  }

  displayHost(value: string | null | undefined): string {
    const safe = this.safeWebsite(value);
    if (!safe) return value?.trim() || 'No website';
    try {
      return new URL(safe).hostname || safe;
    } catch {
      return safe;
    }
  }

  private buildRequest(): CreateCredentialRequest | null {
    const value = this.form.getRawValue();
    const title = value.title.trim();
    const username = value.username.trim();
    const password = value.password;
    const website = value.website.trim();
    const notes = value.notes.trim();

    if (!title || !username || !password) return null;
    if (website && !this.safeWebsite(website)) {
      this.form.controls.website.setErrors({ unsafeUrl: true });
      this.form.controls.website.markAsTouched();
      this.notifications.warning('Website must start with a valid http:// or https:// address.');
      return null;
    }

    return {
      title,
      username,
      password,
      website: website || null,
      notes: notes || null
    };
  }

  private resetForm(): void {
    this.editingCredentialId.set(null);
    this.passwordVisible.set(false);
    this.form.reset({ title: '', username: '', password: '', website: '', notes: '' });
    this.form.markAsPristine();
    this.form.markAsUntouched();
  }

  private clearSensitiveState(): void {
    this.resetForm();
    this.hideRevealed();
  }

  private startRevealExpiry(): void {
    this.clearRevealTimers();
    const lifetimeSeconds = 60;
    this.revealExpiresIn.set(lifetimeSeconds);

    this.revealCountdownTimer = window.setInterval(() => {
      const current = this.revealExpiresIn();
      if (current === null || current <= 1) {
        this.hideRevealed();
        return;
      }
      this.revealExpiresIn.set(current - 1);
    }, 1000);

    this.revealTimer = window.setTimeout(() => {
      this.hideRevealed();
      this.notifications.show('Revealed credential hidden automatically.', 'info', 3200);
    }, lifetimeSeconds * 1000);
  }

  private clearRevealTimers(): void {
    if (this.revealTimer !== null) window.clearTimeout(this.revealTimer);
    if (this.revealCountdownTimer !== null) window.clearInterval(this.revealCountdownTimer);
    this.revealTimer = null;
    this.revealCountdownTimer = null;
  }

  private async copyText(value: string | null | undefined, successMessage: string): Promise<void> {
    if (!value) {
      this.notifications.warning('Nothing to copy.');
      return;
    }
    try {
      if (!navigator.clipboard?.writeText) throw new Error('Clipboard unavailable');
      await navigator.clipboard.writeText(value);
      this.notifications.success(successMessage);
    } catch {
      this.notifications.error('Clipboard access is unavailable. Select and copy the value manually.');
    }
  }

  private validId(id: number): boolean {
    return Number.isInteger(id) && id > 0;
  }

  private message(error: unknown, fallback: string): string {
    return error instanceof Error && error.message.trim() ? error.message : fallback;
  }
}
