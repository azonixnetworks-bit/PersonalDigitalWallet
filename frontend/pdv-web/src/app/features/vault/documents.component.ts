import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  OnInit,
  ViewChild,
  computed,
  signal
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import {
  DocumentDto,
  DocumentShareDto,
  FolderDto,
  SecureUploadProgress
} from '../../core/models/vault.models';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { UiEmptyStateComponent } from '../../shared/components/ui-empty-state/ui-empty-state.component';
import { UiLoaderComponent } from '../../shared/components/ui-loader/ui-loader.component';
import { NotificationService } from '../../shared/services/notification.service';
import { DocumentService } from './document.service';
import { FolderService } from './folder.service';
import { formatFileSize, formatVaultDate, safeDownloadName } from './vault-formatters';

const MAX_FILE_SIZE = 10 * 1024 * 1024;
const ALLOWED_EXTENSIONS = ['.pdf', '.doc', '.docx', '.jpg', '.jpeg', '.png'];

@Component({
  selector: 'app-documents',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, PageHeaderComponent, UiEmptyStateComponent, UiLoaderComponent],
  templateUrl: './documents.component.html',
  styleUrl: './documents.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DocumentsComponent implements OnInit {
  @ViewChild('fileInput') private fileInput?: ElementRef<HTMLInputElement>;

  readonly documents = signal<DocumentDto[]>([]);
  readonly folders = signal<FolderDto[]>([]);
  readonly loadingDocuments = signal(true);
  readonly loadingFolders = signal(true);
  readonly refreshing = signal(false);
  readonly documentsError = signal('');
  readonly foldersError = signal('');
  readonly routeFolderId = signal<number | null>(null);

  readonly selectedFile = signal<File | null>(null);
  readonly uploadLocation = signal<'root' | 'folder'>('root');
  readonly selectedFolderId = signal<number | null>(null);
  readonly uploading = signal(false);
  readonly uploadProgress = signal<SecureUploadProgress>({
    stage: 'idle', percent: 0, title: 'Waiting to start', detail: ''
  });

  readonly filterMode = signal('all');
  readonly actionBusyId = signal<number | null>(null);
  readonly details = signal<DocumentDto | null>(null);
  readonly focusedDocumentId = signal<number | null>(null);

  readonly shareDocument = signal<DocumentDto | null>(null);
  readonly shares = signal<DocumentShareDto[]>([]);
  readonly loadingShares = signal(false);
  readonly shareBusy = signal(false);
  readonly shareEmail = new FormControl('', {
  nonNullable: true,
  validators: [
    Validators.required,
    Validators.email,
    Validators.maxLength(320)
  ]
});

  readonly foldersAvailable = computed(() => this.folders().length > 0);
  readonly isFolderView = computed(() => this.routeFolderId() !== null);
  readonly currentFolder = computed(() => {
    const id = this.routeFolderId();
    return id === null ? null : this.folders().find(folder => folder.id === id) ?? null;
  });
  readonly folderViewTitle = computed(() => {
    if (!this.isFolderView()) return 'Documents';
    const folder = this.currentFolder();
    if (folder) return folder.name || 'Unnamed folder';
    return this.loadingFolders() ? 'Loading folder...' : 'Folder unavailable';
  });
  readonly uploadDestination = computed(() => {
    if (this.uploadLocation() !== 'folder') return 'Root Vault';
    const folder = this.folders().find(item => item.id === this.selectedFolderId());
    return folder?.name || 'Choose a folder';
  });

  readonly filteredDocuments = computed(() => {
    const mode = this.filterMode();
    const items = this.documents();
    if (mode === 'root') return items.filter(item => item.folderId === null);
    if (mode.startsWith('folder:')) {
      const id = Number(mode.slice(7));
      return items.filter(item => item.folderId === id);
    }
    return items;
  });

  readonly listLabel = computed(() => {
    const mode = this.filterMode();
    if (mode === 'root') return 'Root Vault';
    if (mode.startsWith('folder:')) {
      const id = Number(mode.slice(7));
      return this.folders().find(item => item.id === id)?.name || 'Selected folder';
    }
    return 'All documents';
  });

  readonly fileHint = computed(() => {
    const file = this.selectedFile();
    return file ? `${file.name} • ${formatFileSize(file.size)}` : 'PDF, DOC, DOCX, JPG or PNG • maximum 10 MB';
  });

  constructor(
    private readonly documentsApi: DocumentService,
    private readonly foldersApi: FolderService,
    private readonly notifications: NotificationService,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly destroyRef: DestroyRef
  ) {}

  ngOnInit(): void {
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(params => {
      const folderId = Number(params.get('folderId'));
      const validFolderId = Number.isInteger(folderId) && folderId > 0 ? folderId : null;

      this.routeFolderId.set(validFolderId);

      if (validFolderId !== null) {
        this.filterMode.set(`folder:${validFolderId}`);
        this.uploadLocation.set('folder');
        this.selectedFolderId.set(validFolderId);
      }
    });

    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(params => {
      if (this.routeFolderId() === null) {
        const folderId = Number(params.get('folderId'));
        if (Number.isInteger(folderId) && folderId > 0) {
          this.filterMode.set(`folder:${folderId}`);
        } else if (params.get('location') === 'root') {
          this.filterMode.set('root');
        } else {
          this.filterMode.set('all');
        }
      }

      const documentId = Number(params.get('documentId'));
      if (Number.isInteger(documentId) && documentId > 0) {
        if (this.focusedDocumentId() !== documentId) {
          this.focusedDocumentId.set(documentId);
          this.loadDetailsById(documentId, true);
        }
      } else {
        this.focusedDocumentId.set(null);
      }
    });

    this.loadFolders();
    this.loadDocuments();
  }

  refresh(): void {
    this.refreshing.set(true);
    let pending = 2;
    const done = () => {
      pending -= 1;
      if (pending <= 0) this.refreshing.set(false);
    };
    this.loadFolders(done);
    this.loadDocuments(done);
  }

  loadFolders(onDone?: () => void): void {
    this.loadingFolders.set(true);
    this.foldersError.set('');
    this.foldersApi.list().pipe(finalize(() => {
      this.loadingFolders.set(false);
      onDone?.();
    })).subscribe({
      next: folders => {
        const items = Array.isArray(folders) ? folders : [];
        this.folders.set(items);

        const lockedFolderId = this.routeFolderId();
        if (lockedFolderId !== null) {
          this.filterMode.set(`folder:${lockedFolderId}`);

          if (items.some(folder => folder.id === lockedFolderId)) {
            this.uploadLocation.set('folder');
            this.selectedFolderId.set(lockedFolderId);
          } else {
            this.selectedFolderId.set(null);
            this.foldersError.set('This folder is unavailable or you no longer have access to it.');
          }

          return;
        }

        if (!items.length && this.uploadLocation() === 'folder') {
          this.setUploadLocation('root');
        }
      },
      error: error => {
        this.folders.set([]);
        this.foldersError.set(this.message(error, 'Folders could not be loaded. Root Vault upload is still available.'));

        if (this.routeFolderId() !== null) {
          this.selectedFolderId.set(null);
          return;
        }

        this.setUploadLocation('root');
      }
    });
  }

  loadDocuments(onDone?: () => void): void {
    this.loadingDocuments.set(true);
    this.documentsError.set('');
    this.documentsApi.list().pipe(finalize(() => {
      this.loadingDocuments.set(false);
      onDone?.();
    })).subscribe({
      next: documents => this.documents.set(Array.isArray(documents) ? documents : []),
      error: error => this.documentsError.set(this.message(error, 'Documents could not be loaded.'))
    });
  }

  chooseFile(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.item(0) || null;
    this.selectedFile.set(file);
    this.resetProgress();
    if (!file) return;
    const validation = this.validateFile(file);
    if (validation) {
      this.notifications.warning(validation);
    }
  }

  setUploadLocation(location: 'root' | 'folder'): void {
    if (this.isFolderView()) return;
    if (location === 'folder' && !this.foldersAvailable()) return;
    this.uploadLocation.set(location);
    if (location === 'root') this.selectedFolderId.set(null);
  }

  setSelectedFolder(value: string): void {
    if (this.isFolderView()) return;
    const id = Number(value);
    this.selectedFolderId.set(Number.isInteger(id) && id > 0 ? id : null);
  }

  upload(): void {
    const file = this.selectedFile();
    if (!file || this.uploading()) {
      if (!file) this.notifications.warning('Please select a file.');
      return;
    }

    const validation = this.validateFile(file);
    if (validation) {
      this.notifications.warning(validation);
      return;
    }

    if (this.isFolderView() && !this.currentFolder()) {
      this.notifications.warning('This folder is unavailable. Return to Folders and choose an available folder.');
      return;
    }

    const folderId = this.isFolderView()
      ? this.routeFolderId()
      : (this.uploadLocation() === 'folder' ? this.selectedFolderId() : null);

    if ((this.isFolderView() || this.uploadLocation() === 'folder') && folderId === null) {
      this.notifications.warning('Please choose a folder, or select Root Vault.');
      return;
    }

    this.uploading.set(true);
    this.uploadProgress.set({
      stage: 'preparing', percent: 0, title: 'Preparing secure upload…',
      detail: `Preparing ${file.name} for secure transfer.`
    });

    this.documentsApi.upload(file, folderId).pipe(
      finalize(() => this.uploading.set(false))
    ).subscribe({
      next: event => {
        if (event.type === 'progress') {
          this.uploadProgress.set({
            stage: 'uploading',
            percent: event.securePercent,
            title: 'Uploading securely…',
            detail: `${event.uploadPercent}% of the file has reached the secure server.`
          });
          return;
        }

        if (event.type === 'processing') {
          this.uploadProgress.set({
            stage: 'encrypting', percent: 90, title: 'Encrypting document…',
            detail: 'Upload complete. The server is validating, hashing, AES-encrypting and saving the protected vault file.'
          });
          return;
        }

        this.uploadProgress.set({
          stage: 'complete', percent: 100, title: 'File encrypted',
          detail: 'Encryption completed successfully. The encrypted vault file has been stored securely.'
        });
        this.documents.update(items => [event.document, ...items.filter(item => item.id !== event.document.id)]);
        this.clearUploadSelection();
        const destination = folderId === null ? 'Root Vault' : this.folderName(folderId);
        this.notifications.success(`File encrypted and stored securely in ${destination}.`);
      },
      error: error => {
        const reason = this.message(error, 'Document could not be uploaded.');
        this.uploadProgress.update(progress => ({
          ...progress,
          stage: 'error',
          title: 'Secure upload failed',
          detail: reason
        }));
        this.notifications.error(reason);
      }
    });
  }

  setFilter(value: string): void {
    if (this.isFolderView()) return;
    this.filterMode.set(value);
    if (value === 'root') {
      void this.router.navigate([], { relativeTo: this.route, queryParams: { location: 'root', folderId: null }, queryParamsHandling: 'merge' });
      return;
    }
    if (value.startsWith('folder:')) {
      const id = Number(value.slice(7));
      void this.router.navigate([], { relativeTo: this.route, queryParams: { folderId: id, location: null }, queryParamsHandling: 'merge' });
      return;
    }
    void this.router.navigate([], { relativeTo: this.route, queryParams: { folderId: null, location: null }, queryParamsHandling: 'merge' });
  }

  showDetails(document: DocumentDto): void {
    this.focusedDocumentId.set(document.id);
    this.loadDetailsById(document.id, true);
  }

  private loadDetailsById(documentId: number, scrollIntoView = false): void {
    if (!Number.isInteger(documentId) || documentId <= 0 || this.actionBusyId() !== null) return;

    this.actionBusyId.set(documentId);
    this.documentsApi.get(documentId).pipe(finalize(() => this.actionBusyId.set(null))).subscribe({
      next: details => {
        this.details.set(details);
        if (scrollIntoView) {
          window.requestAnimationFrame(() => document.getElementById('document-details-panel')?.scrollIntoView({ behavior: 'smooth', block: 'start' }));
        }
      },
      error: error => this.notifications.error(this.message(error, 'Document details could not be loaded.'))
    });
  }

  closeDetails(): void {
    this.details.set(null);
  }

  download(document: DocumentDto): void {
    this.actionBusyId.set(document.id);
    this.documentsApi.download(document.id).pipe(finalize(() => this.actionBusyId.set(null))).subscribe({
      next: blob => {
        const url = URL.createObjectURL(blob);
        const link = window.document.createElement('a');
        link.href = url;
        link.download = safeDownloadName(document.fileName);
        window.document.body.appendChild(link);
        link.click();
        link.remove();
        window.setTimeout(() => URL.revokeObjectURL(url), 1000);
        this.notifications.success('Document downloaded successfully.');
      },
      error: error => this.notifications.error(this.message(error, 'Document could not be downloaded.'))
    });
  }

  rename(document: DocumentDto): void {
    const input = window.prompt('Enter the new file name:', document.fileName || '');
    if (input === null) return;
    const fileName = input.trim();
    if (!fileName) {
      this.notifications.warning('File name cannot be empty.');
      return;
    }
    if (fileName === document.fileName) return;

    this.actionBusyId.set(document.id);
    this.documentsApi.rename(document.id, { fileName }).pipe(finalize(() => this.actionBusyId.set(null))).subscribe({
      next: updated => {
        this.documents.update(items => items.map(item => item.id === updated.id ? updated : item));
        if (this.details()?.id === updated.id) this.details.set(updated);
        if (this.shareDocument()?.id === updated.id) this.shareDocument.set(updated);
        this.notifications.success('Document renamed successfully.');
      },
      error: error => this.notifications.error(this.message(error, 'Document could not be renamed.'))
    });
  }

  remove(document: DocumentDto): void {
    const confirmed = window.confirm(`Delete "${document.fileName}" permanently from your vault? This cannot be undone.`);
    if (!confirmed) return;
    this.actionBusyId.set(document.id);
    this.documentsApi.remove(document.id).pipe(finalize(() => this.actionBusyId.set(null))).subscribe({
      next: () => {
        this.documents.update(items => items.filter(item => item.id !== document.id));
        if (this.details()?.id === document.id) this.details.set(null);
        if (this.shareDocument()?.id === document.id) this.closeSharePanel();
        this.notifications.success('Document deleted successfully.');
      },
      error: error => this.notifications.error(this.message(error, 'Document could not be deleted.'))
    });
  }

  openSharePanel(document: DocumentDto, focusEmail = false): void {
    this.shareDocument.set(document);
    this.shareEmail.reset('');
    this.loadShares(document.id);
    if (focusEmail) {
      window.setTimeout(() => window.document.getElementById('recipientEmail')?.focus(), 0);
    }
  }

  closeSharePanel(): void {
    this.shareDocument.set(null);
    this.shares.set([]);
    this.shareEmail.reset('');
  }

 sendShare(): void {
  const document = this.shareDocument();

  this.shareEmail.markAsTouched();

  const recipientEmail =
    this.shareEmail.value.trim();

  if (
    !document ||
    this.shareEmail.invalid ||
    !recipientEmail ||
    this.shareBusy()
  ) {
    return;
  }

  this.shareBusy.set(true);

  this.documentsApi
    .share(document.id, { recipientEmail })
    .pipe(
      finalize(() =>
        this.shareBusy.set(false)
      )
    )
    .subscribe({
      next: share => {
        this.shares.update(items => [
          share,
          ...items.filter(
            item =>
              item.shareId !== share.shareId
          )
        ]);

        this.shareEmail.reset('');

        this.notifications.success(
          `Secure invitation sent to registered PDV client ${share.recipientEmail || recipientEmail}.`
        );
      },

      error: () => {
        this.notifications.error(
          'This document could not be shared. Only an eligible registered PDV client can receive secure document access.'
        );
      }
    });
}

  loadShares(documentId: number): void {
    this.loadingShares.set(true);
    this.documentsApi.listShares(documentId).pipe(finalize(() => this.loadingShares.set(false))).subscribe({
      next: shares => this.shares.set(Array.isArray(shares) ? shares : []),
      error: error => {
        this.shares.set([]);
        this.notifications.error(this.message(error, 'Share status could not be loaded.'));
      }
    });
  }

  revokeShare(share: DocumentShareDto): void {
    const document = this.shareDocument();
    if (!document || this.shareBusy()) return;
    const confirmed = window.confirm(`Revoke access for ${share.recipientEmail || 'this recipient'}?`);
    if (!confirmed) return;

    this.shareBusy.set(true);
    this.documentsApi.revokeShare(document.id, share.shareId).pipe(finalize(() => this.shareBusy.set(false))).subscribe({
      next: () => {
        this.shares.update(items => items.map(item => item.shareId === share.shareId
          ? { ...item, status: 'Revoked', revokedAt: new Date().toISOString() }
          : item));
        this.notifications.success('Document share revoked successfully.');
      },
      error: error => this.notifications.error(this.message(error, 'Document share could not be revoked.'))
    });
  }

  folderName(folderId: number | null): string {
    if (folderId === null) return 'Root Vault';
    return this.folders().find(folder => folder.id === folderId)?.name || `Folder ${folderId}`;
  }

  formatSize(bytes: number): string { return formatFileSize(bytes); }
  formatDate(value: string | null | undefined): string { return formatVaultDate(value); }

  isShareRevocable(share: DocumentShareDto): boolean {
    return (share.status || '').toLowerCase() !== 'revoked';
  }

  private clearUploadSelection(): void {
    this.selectedFile.set(null);

    if (this.isFolderView()) {
      this.uploadLocation.set('folder');
      this.selectedFolderId.set(this.routeFolderId());
    } else {
      this.uploadLocation.set('root');
      this.selectedFolderId.set(null);
    }

    if (this.fileInput?.nativeElement) this.fileInput.nativeElement.value = '';
  }

  private resetProgress(): void {
    this.uploadProgress.set({ stage: 'idle', percent: 0, title: 'Waiting to start', detail: '' });
  }

  private validateFile(file: File): string | null {
    if (file.size <= 0) return 'The selected file is empty.';
    if (file.size > MAX_FILE_SIZE) return 'The selected file must not exceed 10 MB.';
    const lower = file.name.toLowerCase();
    if (!ALLOWED_EXTENSIONS.some(extension => lower.endsWith(extension))) {
      return 'Unsupported file type. Choose PDF, DOC, DOCX, JPG, JPEG or PNG.';
    }
    return null;
  }

  private message(error: unknown, fallback: string): string {
    return error instanceof Error && error.message.trim() ? error.message : fallback;
  }
}
