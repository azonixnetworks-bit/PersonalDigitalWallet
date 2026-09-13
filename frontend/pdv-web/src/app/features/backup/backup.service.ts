import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiClientService } from '../../core/api/api-client.service';

export interface BackupStatus {
  isConfigured: boolean;
  isConnected: boolean;
  provider: string;
  accountEmail: string | null;
  autoBackupEnabled: boolean;
  autoBackupFrequency: 'Daily' | 'Weekly' | 'Monthly';
  lastBackupAt: string | null;
  nextBackupAt: string | null;
}

export interface BackupHistoryItem {
  fileId: string;
  fileName: string;
  size: number;
  createdAt: string | null;
  integrityHash: string | null;
  status: string;
}

export interface RestoreBackupResult {
  foldersRestored: number;
  documentsRestored: number;
  credentialsRestored: number;
  itemsSkipped: number;
  message: string;
}

interface ConnectUrlResponse {
  url: string;
}

interface ActionResponse {
  message: string;
}

@Injectable({ providedIn: 'root' })
export class BackupService {
  constructor(private readonly api: ApiClientService) {}

  status(): Observable<BackupStatus> {
    return this.api.get<BackupStatus>('backup/status');
  }

  connectUrl(): Observable<ConnectUrlResponse> {
    return this.api.get<ConnectUrlResponse>('backup/google/connect-url');
  }

  backupNow(): Observable<BackupHistoryItem> {
    return this.api.post<BackupHistoryItem>('backup/now');
  }

  history(): Observable<BackupHistoryItem[]> {
    return this.api.get<BackupHistoryItem[]>('backup/history');
  }

  setAutomatic(enabled: boolean, frequency: 'Daily' | 'Weekly' | 'Monthly'): Observable<ActionResponse> {
    return this.api.put<ActionResponse>('backup/automatic', { enabled, frequency });
  }

  restore(fileId: string, totpCode: string): Observable<RestoreBackupResult> {
    return this.api.post<RestoreBackupResult>('backup/restore', { fileId, totpCode });
  }

  disconnect(): Observable<ActionResponse> {
    return this.api.delete<ActionResponse>('backup/google');
  }
}
