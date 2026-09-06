import { Injectable } from '@angular/core';
import { Observable, map, of } from 'rxjs';
import { ApiClientService } from '../../core/api/api-client.service';
import { VaultSearchResult, VaultSearchResultType } from '../../core/models/search-sharing.models';

@Injectable({ providedIn: 'root' })
export class SearchService {
  constructor(private readonly api: ApiClientService) {}

  search(keyword: string): Observable<VaultSearchResult[]> {
    const cleanKeyword = keyword.trim();
    if (!cleanKeyword) {
      return of([]);
    }

    return this.api.get<unknown[]>('/search', {
      params: { keyword: cleanKeyword }
    }).pipe(
      map(value => Array.isArray(value)
        ? value.map(item => this.sanitize(item)).filter((item): item is VaultSearchResult => item !== null)
        : [])
    );
  }

  private sanitize(value: unknown): VaultSearchResult | null {
    if (!value || typeof value !== 'object') return null;

    const candidate = value as Record<string, unknown>;
    const id = Number(candidate['id']);
    const title = typeof candidate['title'] === 'string' ? candidate['title'].trim() : '';
    const type = this.normalizeType(candidate['type']);

    if (!Number.isInteger(id) || id <= 0 || !title || !type) return null;
    return { id, title, type };
  }

  private normalizeType(value: unknown): VaultSearchResultType | null {
    if (typeof value !== 'string') return null;
    const normalized = value.trim().toLowerCase();
    if (normalized === 'folder') return 'Folder';
    if (normalized === 'document') return 'Document';
    if (normalized === 'credential') return 'Credential';
    return null;
  }
}
