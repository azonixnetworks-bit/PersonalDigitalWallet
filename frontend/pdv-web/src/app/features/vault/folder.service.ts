import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiClientService } from '../../core/api/api-client.service';
import { CreateFolderRequest, FolderDto, UpdateFolderRequest } from '../../core/models/vault.models';

@Injectable({ providedIn: 'root' })
export class FolderService {
  constructor(private readonly api: ApiClientService) {}

  list(): Observable<FolderDto[]> {
    return this.api.get<FolderDto[]>('/folders');
  }

  create(request: CreateFolderRequest): Observable<FolderDto> {
    return this.api.post<FolderDto>('/folders', request);
  }

  update(id: number, request: UpdateFolderRequest): Observable<FolderDto> {
    return this.api.put<FolderDto>(`/folders/${id}`, request);
  }

  remove(id: number): Observable<void> {
    return this.api.delete<void>(`/folders/${id}`);
  }
}
