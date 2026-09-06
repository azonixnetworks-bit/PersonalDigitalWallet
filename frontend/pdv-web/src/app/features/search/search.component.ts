import { ChangeDetectionStrategy, Component, computed, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import {
  VaultSearchFilter,
  VaultSearchResult
} from '../../core/models/search-sharing.models';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { UiEmptyStateComponent } from '../../shared/components/ui-empty-state/ui-empty-state.component';
import { UiLoaderComponent } from '../../shared/components/ui-loader/ui-loader.component';
import { SearchService } from './search.service';

@Component({
  selector: 'app-search',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent, UiEmptyStateComponent, UiLoaderComponent],
  templateUrl: './search.component.html',
  styleUrl: './search.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SearchComponent {
  readonly keyword = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.maxLength(250)]
  });

  readonly results = signal<VaultSearchResult[]>([]);
  readonly loading = signal(false);
  readonly searched = signal(false);
  readonly error = signal('');
  readonly lastKeyword = signal('');
  readonly typeFilter = signal<VaultSearchFilter>('all');

  readonly filteredResults = computed(() => {
    const filter = this.typeFilter();
    if (filter === 'all') return this.results();
    return this.results().filter(item => item.type.toLowerCase() === filter);
  });

  readonly resultCountLabel = computed(() => {
    const total = this.results().length;
    const visible = this.filteredResults().length;
    if (!this.searched()) return '';
    if (this.typeFilter() !== 'all') return `${visible} shown • ${total} total`;
    return `${total} ${total === 1 ? 'result' : 'results'}`;
  });

  constructor(
    private readonly searchApi: SearchService,
    private readonly router: Router
  ) {}

  search(): void {
    if (this.loading()) return;

    this.keyword.markAsTouched();
    const cleanKeyword = this.keyword.value.trim();
    if (this.keyword.invalid || !cleanKeyword) {
      this.results.set([]);
      this.searched.set(false);
      this.error.set('Enter a folder name, document filename or credential title.');
      return;
    }

    this.loading.set(true);
    this.error.set('');
    this.searched.set(true);
    this.lastKeyword.set(cleanKeyword);

    this.searchApi.search(cleanKeyword).pipe(
      finalize(() => this.loading.set(false))
    ).subscribe({
      next: results => this.results.set(results),
      error: error => {
        this.results.set([]);
        this.error.set(this.message(error, 'Search could not be completed.'));
      }
    });
  }

  setFilter(value: string): void {
    if (value === 'folder' || value === 'document' || value === 'credential') {
      this.typeFilter.set(value);
      return;
    }
    this.typeFilter.set('all');
  }

  openResult(result: VaultSearchResult): void {
    if (result.type === 'Folder') {
      void this.router.navigate(['/vault/documents'], { queryParams: { folderId: result.id } });
      return;
    }

    if (result.type === 'Document') {
      void this.router.navigate(['/vault/documents'], { queryParams: { documentId: result.id } });
      return;
    }

    void this.router.navigate(['/vault/credentials'], { queryParams: { credentialId: result.id } });
  }

  icon(type: VaultSearchResult['type']): string {
    if (type === 'Folder') return '📁';
    if (type === 'Document') return '📄';
    return '🔐';
  }

  private message(error: unknown, fallback: string): string {
    return error instanceof Error && error.message.trim() ? error.message : fallback;
  }
}
