import { ChangeDetectionStrategy, Component, OnInit, computed, signal } from '@angular/core';
import { finalize } from 'rxjs';
import { SharedDocumentDto } from '../../core/models/search-sharing.models';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { UiEmptyStateComponent } from '../../shared/components/ui-empty-state/ui-empty-state.component';
import { UiLoaderComponent } from '../../shared/components/ui-loader/ui-loader.component';
import { NotificationService } from '../../shared/services/notification.service';
import { DocumentService } from '../vault/document.service';
import { formatFileSize, formatVaultDate, safeDownloadName } from '../vault/vault-formatters';
import { ShareInvitationStorageService } from './share-invitation-storage.service';
import { SharingService } from './sharing.service';

@Component({
  selector: 'app-shared-with-me',
  standalone: true,
  imports: [PageHeaderComponent, UiEmptyStateComponent, UiLoaderComponent],
  templateUrl: './shared-with-me.component.html',
  styleUrl: './shared-with-me.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SharedWithMeComponent implements OnInit {
  readonly documents = signal<SharedDocumentDto[]>([]);
  readonly loading = signal(true);
  readonly refreshing = signal(false);
  readonly error = signal('');
  readonly downloadingId = signal<number | null>(null);
  readonly searchTerm = signal('');

  readonly filteredDocuments = computed(() => {
    const query = this.searchTerm().trim().toLowerCase();
    if (!query) return this.documents();
    return this.documents().filter(item => [item.fileName, item.sharedBy, item.sharedByEmail]
      .join(' ')
      .toLowerCase()
      .includes(query));
  });

  readonly countLabel = computed(() => {
    const total = this.documents().length;
    const visible = this.filteredDocuments().length;
    return this.searchTerm().trim() ? `${visible} of ${total} shared documents` : `${total} ${total === 1 ? 'document' : 'documents'}`;
  });

  constructor(
    private readonly sharingApi: SharingService,
    private readonly documentsApi: DocumentService,
    private readonly invitationStorage: ShareInvitationStorageService,
    private readonly notifications: NotificationService
  ) {}

  ngOnInit(): void {
    const success = this.invitationStorage.consumeSuccess();
    if (success) this.notifications.success(success);
    this.load();
  }

  load(refresh = false): void {
    this.error.set('');
    refresh ? this.refreshing.set(true) : this.loading.set(true);

    this.sharingApi.sharedWithMe().pipe(
      finalize(() => {
        this.loading.set(false);
        this.refreshing.set(false);
      })
    ).subscribe({
      next: documents => this.documents.set(Array.isArray(documents) ? documents : []),
      error: error => this.error.set(this.message(error, 'Shared documents could not be loaded.'))
    });
  }

  setSearch(value: string): void {
    this.searchTerm.set(value);
  }

  download(item: SharedDocumentDto): void {
    if (!Number.isInteger(item.documentId) || item.documentId <= 0 || this.downloadingId() !== null) return;

    this.downloadingId.set(item.documentId);
    this.documentsApi.download(item.documentId).pipe(
      finalize(() => this.downloadingId.set(null))
    ).subscribe({
      next: blob => this.saveBlob(blob, item.fileName),
      error: error => this.notifications.error(this.message(error, 'Shared document could not be downloaded.'))
    });
  }

  fileSize(bytes: number): string {
    return formatFileSize(bytes);
  }

  date(value: string | null): string {
    return value ? formatVaultDate(value) : '—';
  }

  fileKind(contentType: string): string {
    const type = (contentType || '').toLowerCase();
    if (type.includes('pdf')) return 'PDF';
    if (type.includes('word') || type.includes('document')) return 'DOC';
    if (type.startsWith('image/')) return 'IMAGE';
    return 'FILE';
  }

  private saveBlob(blob: Blob, fileName: string): void {
    const url = URL.createObjectURL(blob);
    try {
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = safeDownloadName(fileName || 'shared-document');
      anchor.rel = 'noopener';
      anchor.click();
      this.notifications.success('Secure download started.');
    } finally {
      window.setTimeout(() => URL.revokeObjectURL(url), 1000);
    }
  }

  private message(error: unknown, fallback: string): string {
    return error instanceof Error && error.message.trim() ? error.message : fallback;
  }
}
