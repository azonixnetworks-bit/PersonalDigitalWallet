import { ChangeDetectionStrategy, Component, OnInit, computed, signal } from '@angular/core';
import { ReactiveFormsModule, FormControl, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { FolderDto } from '../../core/models/vault.models';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { UiEmptyStateComponent } from '../../shared/components/ui-empty-state/ui-empty-state.component';
import { UiLoaderComponent } from '../../shared/components/ui-loader/ui-loader.component';
import { NotificationService } from '../../shared/services/notification.service';
import { FolderService } from './folder.service';
import { formatVaultDate } from './vault-formatters';

@Component({
  selector: 'app-folders',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, PageHeaderComponent, UiEmptyStateComponent, UiLoaderComponent],
  templateUrl: './folders.component.html',
  styleUrl: './folders.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FoldersComponent implements OnInit {
  readonly folders = signal<FolderDto[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly refreshing = signal(false);
  readonly error = signal('');
  readonly busyFolderId = signal<number | null>(null);
  readonly editingFolderId = signal<number | null>(null);
  readonly editName = signal('');

  readonly folderName = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.maxLength(200)]
  });

  readonly folderCountLabel = computed(() => {
    const count = this.folders().length;
    return `${count} ${count === 1 ? 'folder' : 'folders'}`;
  });

  constructor(
    private readonly foldersApi: FolderService,
    private readonly notifications: NotificationService
  ) {}

  ngOnInit(): void {
    this.loadFolders();
  }

  loadFolders(refresh = false): void {
    this.error.set('');
    refresh ? this.refreshing.set(true) : this.loading.set(true);

    this.foldersApi.list().pipe(
      finalize(() => {
        this.loading.set(false);
        this.refreshing.set(false);
      })
    ).subscribe({
      next: folders => this.folders.set(Array.isArray(folders) ? folders : []),
      error: error => this.error.set(this.message(error, 'Folders could not be loaded.'))
    });
  }

  createFolder(): void {
    this.folderName.markAsTouched();
    const name = this.folderName.value.trim();
    if (!name || this.folderName.invalid || this.saving()) return;

    this.saving.set(true);
    this.foldersApi.create({ name }).pipe(
      finalize(() => this.saving.set(false))
    ).subscribe({
      next: folder => {
        this.folders.update(items => [folder, ...items.filter(item => item.id !== folder.id)]);
        this.folderName.reset('');
        this.notifications.success('Folder created successfully.');
      },
      error: error => this.notifications.error(this.message(error, 'Folder could not be created.'))
    });
  }

  startRename(folder: FolderDto): void {
    if (this.busyFolderId() !== null) return;
    this.editingFolderId.set(folder.id);
    this.editName.set(folder.name || '');
  }

  cancelRename(): void {
    this.editingFolderId.set(null);
    this.editName.set('');
  }

  setEditName(value: string): void {
    this.editName.set(value);
  }

  saveRename(folder: FolderDto): void {
    const name = this.editName().trim();
    if (!name) {
      this.notifications.warning('Folder name cannot be empty.');
      return;
    }
    if (name.length > 200) {
      this.notifications.warning('Folder name is too long.');
      return;
    }
    if (name === folder.name) {
      this.cancelRename();
      return;
    }

    this.busyFolderId.set(folder.id);
    this.foldersApi.update(folder.id, { name }).pipe(
      finalize(() => this.busyFolderId.set(null))
    ).subscribe({
      next: updated => {
        this.folders.update(items => items.map(item => item.id === updated.id ? updated : item));
        this.cancelRename();
        this.notifications.success('Folder renamed successfully.');
      },
      error: error => this.notifications.error(this.message(error, 'Folder could not be renamed.'))
    });
  }

  deleteFolder(folder: FolderDto): void {
    if (this.busyFolderId() !== null) return;
    const name = folder.name || 'this folder';
    const confirmed = window.confirm(`Delete "${name}"? Documents inside the folder will remain in your vault and move to Root Vault.`);
    if (!confirmed) return;

    this.busyFolderId.set(folder.id);
    this.foldersApi.remove(folder.id).pipe(
      finalize(() => this.busyFolderId.set(null))
    ).subscribe({
      next: () => {
        this.folders.update(items => items.filter(item => item.id !== folder.id));
        if (this.editingFolderId() === folder.id) this.cancelRename();
        this.notifications.success('Folder deleted. Its documents remain safely stored in Root Vault.');
      },
      error: error => this.notifications.error(this.message(error, 'Folder could not be deleted.'))
    });
  }

  formatDate(value: string): string {
    return formatVaultDate(value);
  }

  private message(error: unknown, fallback: string): string {
    return error instanceof Error && error.message.trim() ? error.message : fallback;
  }
}
