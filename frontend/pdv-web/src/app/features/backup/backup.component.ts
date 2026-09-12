import { ChangeDetectionStrategy, Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { UiEmptyStateComponent } from '../../shared/components/ui-empty-state/ui-empty-state.component';
import { UiLoaderComponent } from '../../shared/components/ui-loader/ui-loader.component';
import { NotificationService } from '../../shared/services/notification.service';
import { BackupHistoryItem, BackupService, BackupStatus } from './backup.service';

@Component({
  selector: 'app-backup',
  standalone: true,
  imports: [FormsModule, PageHeaderComponent, UiEmptyStateComponent, UiLoaderComponent],
  templateUrl: './backup.component.html',
  styleUrl: './backup.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BackupComponent implements OnInit {
  readonly status = signal<BackupStatus | null>(null);
  readonly history = signal<BackupHistoryItem[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly connecting = signal(false);
  readonly backingUp = signal(false);
  readonly savingAutomatic = signal(false);
  readonly disconnecting = signal(false);
  readonly restoring = signal(false);
  readonly restoreTarget = signal<BackupHistoryItem | null>(null);
  readonly restoreTotpCode = signal('');
  readonly automaticEnabled = signal(false);
  readonly frequency = signal<'Daily' | 'Weekly' | 'Monthly'>('Weekly');

  readonly connected = computed(() => this.status()?.isConnected === true);
  readonly configured = computed(() => this.status()?.isConfigured === true);
  readonly canRestore = computed(() =>
    this.restoreTarget() !== null && /^\d{6}$/.test(this.restoreTotpCode()) && !this.restoring());

  constructor(
    private readonly backupApi: BackupService,
    private readonly notifications: NotificationService,
    private readonly route: ActivatedRoute,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    const googleResult = this.route.snapshot.queryParamMap.get('google');
    if (googleResult === 'connected') {
      this.notifications.success('Google Drive connected securely.');
      this.clearGoogleResult();
    } else if (googleResult === 'error') {
      this.notifications.error('Google Drive connection could not be completed.');
      this.clearGoogleResult();
    }

    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');

    this.backupApi.status().pipe(
      finalize(() => this.loading.set(false))
    ).subscribe({
      next: status => {
        this.applyStatus(status);
        if (status.isConnected) this.loadHistory();
        else this.history.set([]);
      },
      error: error => this.error.set(this.message(error, 'Backup settings could not be loaded.'))
    });
  }

  connectGoogleDrive(): void {
    if (this.connecting() || !this.configured()) return;

    this.connecting.set(true);
    this.backupApi.connectUrl().pipe(
      finalize(() => this.connecting.set(false))
    ).subscribe({
      next: result => {
        try {
          const url = new URL(result.url);
          if (url.protocol !== 'https:' || url.hostname !== 'accounts.google.com') {
            throw new Error('Unexpected Google authorization URL.');
          }
          window.location.assign(url.toString());
        } catch {
          this.notifications.error('Google Drive authorization URL is invalid.');
        }
      },
      error: error => this.notifications.error(this.message(error, 'Google Drive could not be connected.'))
    });
  }

  backupNow(): void {
    if (!this.connected() || this.backingUp()) return;

    this.backingUp.set(true);
    this.backupApi.backupNow().pipe(
      finalize(() => this.backingUp.set(false))
    ).subscribe({
      next: () => {
        this.notifications.success('Encrypted vault backup uploaded to Google Drive.');
        this.refreshConnectedData();
      },
      error: error => this.notifications.error(this.message(error, 'Backup could not be completed.'))
    });
  }

  saveAutomaticSettings(): void {
    if (!this.connected() || this.savingAutomatic()) return;

    this.savingAutomatic.set(true);
    this.backupApi.setAutomatic(this.automaticEnabled(), this.frequency()).pipe(
      finalize(() => this.savingAutomatic.set(false))
    ).subscribe({
      next: result => {
        this.notifications.success(result.message || 'Automatic backup settings saved.');
        this.refreshStatus();
      },
      error: error => this.notifications.error(this.message(error, 'Automatic backup settings could not be saved.'))
    });
  }

  beginRestore(item: BackupHistoryItem): void {
    if (this.restoring()) return;
    this.restoreTarget.set(item);
    this.restoreTotpCode.set('');
  }

  cancelRestore(): void {
    if (this.restoring()) return;
    this.restoreTarget.set(null);
    this.restoreTotpCode.set('');
  }

  restoreSelected(): void {
    const target = this.restoreTarget();
    const code = this.restoreTotpCode().trim();
    if (!target || !/^\d{6}$/.test(code) || this.restoring()) return;

    this.restoring.set(true);
    this.backupApi.restore(target.fileId, code).pipe(
      finalize(() => {
        this.restoring.set(false);
        this.restoreTotpCode.set('');
      })
    ).subscribe({
      next: result => {
        this.restoreTarget.set(null);
        this.notifications.success(result.message || 'Backup restored successfully.');
      },
      error: error => this.notifications.error(this.message(error, 'Backup restore failed.'))
    });
  }

  disconnectGoogleDrive(): void {
    if (!this.connected() || this.disconnecting()) return;
    if (!window.confirm('Disconnect Google Drive from PDV? Existing Google Drive backup files will not be deleted.')) return;

    this.disconnecting.set(true);
    this.backupApi.disconnect().pipe(
      finalize(() => this.disconnecting.set(false))
    ).subscribe({
      next: result => {
        this.notifications.success(result.message || 'Google Drive disconnected.');
        this.history.set([]);
        this.load();
      },
      error: error => this.notifications.error(this.message(error, 'Google Drive could not be disconnected.'))
    });
  }

  setAutomaticEnabled(value: boolean): void {
    this.automaticEnabled.set(value);
  }

  setFrequency(value: string): void {
    if (value === 'Daily' || value === 'Weekly' || value === 'Monthly') {
      this.frequency.set(value);
    }
  }

  setRestoreTotpCode(value: string): void {
    const digits = value.replace(/\D/g, '').slice(0, 6);
    this.restoreTotpCode.set(digits);
  }

  fileSize(bytes: number): string {
    if (!Number.isFinite(bytes) || bytes <= 0) return '0 B';
    const units = ['B', 'KB', 'MB', 'GB'];
    let value = bytes;
    let index = 0;
    while (value >= 1024 && index < units.length - 1) {
      value /= 1024;
      index++;
    }
    return `${value >= 10 || index === 0 ? value.toFixed(0) : value.toFixed(1)} ${units[index]}`;
  }

  date(value: string | null | undefined): string {
    if (!value) return '—';
    const parsed = new Date(value);
    return Number.isNaN(parsed.getTime()) ? '—' : parsed.toLocaleString();
  }

  shortHash(hash: string | null): string {
    if (!hash) return '—';
    return hash.length > 20 ? `${hash.slice(0, 12)}…${hash.slice(-8)}` : hash;
  }

  private refreshConnectedData(): void {
    forkJoin({ status: this.backupApi.status(), history: this.backupApi.history() }).subscribe({
      next: result => {
        this.applyStatus(result.status);
        this.history.set(Array.isArray(result.history) ? result.history : []);
      },
      error: error => this.notifications.error(this.message(error, 'Backup status could not be refreshed.'))
    });
  }

  private refreshStatus(): void {
    this.backupApi.status().subscribe({
      next: status => this.applyStatus(status),
      error: error => this.notifications.error(this.message(error, 'Backup status could not be refreshed.'))
    });
  }

  private loadHistory(): void {
    this.backupApi.history().subscribe({
      next: items => this.history.set(Array.isArray(items) ? items : []),
      error: error => this.notifications.error(this.message(error, 'Backup history could not be loaded.'))
    });
  }

  private applyStatus(status: BackupStatus): void {
    this.status.set(status);
    this.automaticEnabled.set(status.autoBackupEnabled === true);
    this.frequency.set(
      status.autoBackupFrequency === 'Daily' || status.autoBackupFrequency === 'Monthly'
        ? status.autoBackupFrequency
        : 'Weekly');
  }

  private clearGoogleResult(): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { google: null },
      queryParamsHandling: 'merge',
      replaceUrl: true
    });
  }

  private message(error: unknown, fallback: string): string {
    if (typeof error === 'object' && error !== null) {
      const candidate = error as { error?: { message?: unknown }; message?: unknown };
      if (typeof candidate.error?.message === 'string' && candidate.error.message.trim()) {
        return candidate.error.message;
      }
      if (typeof candidate.message === 'string' && candidate.message.trim()) {
        return candidate.message;
      }
    }
    return fallback;
  }
}
