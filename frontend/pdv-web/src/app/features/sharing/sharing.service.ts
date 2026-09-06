import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiClientService } from '../../core/api/api-client.service';
import {
  AcceptShareRequest,
  MessageResponse,
  SharedDocumentDto
} from '../../core/models/search-sharing.models';

@Injectable({ providedIn: 'root' })
export class SharingService {
  constructor(private readonly api: ApiClientService) {}

  sharedWithMe(): Observable<SharedDocumentDto[]> {
    return this.api.get<SharedDocumentDto[]>('/shares/shared-with-me');
  }

  accept(token: string): Observable<MessageResponse> {
    const request: AcceptShareRequest = { token: token.trim() };
    return this.api.post<MessageResponse>('/shares/accept', request);
  }
}
