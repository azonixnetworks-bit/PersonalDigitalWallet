import { HttpClient, HttpEvent, HttpEventType, HttpResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, filter, map } from 'rxjs';
import { ApiClientService } from '../../core/api/api-client.service';
import {
  DocumentDto,
  DocumentShareDto,
  DocumentUploadEvent,
  ShareDocumentRequest,
  UpdateDocumentRequest
} from '../../core/models/vault.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class DocumentService {
  private readonly baseUrl = environment.apiBaseUrl.replace(/\/$/, '');

  constructor(
    private readonly api: ApiClientService,
    private readonly http: HttpClient
  ) {}

  list(): Observable<DocumentDto[]> {
    return this.api.get<DocumentDto[]>('/documents');
  }

  get(id: number): Observable<DocumentDto> {
    return this.api.get<DocumentDto>(`/documents/${id}`);
  }

  rename(id: number, request: UpdateDocumentRequest): Observable<DocumentDto> {
    return this.api.put<DocumentDto>(`/documents/${id}`, request);
  }

  remove(id: number): Observable<void> {
    return this.api.delete<void>(`/documents/${id}`);
  }

  upload(file: File, folderId: number | null): Observable<DocumentUploadEvent> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    if (folderId !== null) {
      formData.append('folderId', String(folderId));
    }

    return this.http.post<DocumentDto>(
      `${this.baseUrl}/documents/upload`,
      formData,
      { observe: 'events', reportProgress: true }
    ).pipe(
      map((event: HttpEvent<DocumentDto>): DocumentUploadEvent | null => {
        if (event.type === HttpEventType.UploadProgress) {
          if (!event.total || event.total <= 0) {
            return { type: 'progress', uploadPercent: 0, securePercent: 0 };
          }
          const uploadPercent = Math.min(100, Math.round((event.loaded / event.total) * 100));
          if (uploadPercent >= 100) {
            return { type: 'processing' };
          }
          return {
            type: 'progress',
            uploadPercent,
            securePercent: Math.min(89, Math.round(uploadPercent * 0.9))
          };
        }

        if (event.type === HttpEventType.Response) {
          const response = event as HttpResponse<DocumentDto>;
          if (!response.body) {
            throw new Error('The secure upload completed without document metadata.');
          }
          return { type: 'complete', document: response.body };
        }

        return null;
      }),
      filter((event): event is DocumentUploadEvent => event !== null)
    );
  }

  download(id: number): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/documents/${id}/download`, {
      responseType: 'blob'
    });
  }

  share(documentId: number, request: ShareDocumentRequest): Observable<DocumentShareDto> {
    return this.api.post<DocumentShareDto>(`/documents/${documentId}/share`, request);
  }

  listShares(documentId: number): Observable<DocumentShareDto[]> {
    return this.api.get<DocumentShareDto[]>(`/documents/${documentId}/shares`);
  }

  revokeShare(documentId: number, shareId: number): Observable<{ message: string }> {
    return this.api.delete<{ message: string }>(`/documents/${documentId}/shares/${shareId}`);
  }
}
