import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiClientService } from '../../core/api/api-client.service';
import {
  CredentialDto,
  CreateCredentialRequest,
  UpdateCredentialRequest
} from '../../core/models/vault.models';

@Injectable({ providedIn: 'root' })
export class CredentialService {
  constructor(private readonly api: ApiClientService) {}

  list(): Observable<CredentialDto[]> {
    return this.api.get<CredentialDto[]>('/credentials');
  }

  get(id: number): Observable<CredentialDto> {
    return this.api.get<CredentialDto>(`/credentials/${id}`);
  }

  create(request: CreateCredentialRequest): Observable<CredentialDto> {
    return this.api.post<CredentialDto>('/credentials', request);
  }

  update(id: number, request: UpdateCredentialRequest): Observable<CredentialDto> {
    return this.api.put<CredentialDto>(`/credentials/${id}`, request);
  }

  remove(id: number): Observable<void> {
    return this.api.delete<void>(`/credentials/${id}`);
  }
}
