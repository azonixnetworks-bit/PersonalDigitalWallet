import { Injectable } from '@angular/core';
import { STORAGE_KEYS } from '../../core/storage/storage-keys';

@Injectable({ providedIn: 'root' })
export class ShareInvitationStorageService {
  setPendingToken(token: string): void {
    const clean = token.trim();
    if (!clean || clean.length > 4096) return;
    sessionStorage.setItem(STORAGE_KEYS.pendingShareToken, clean);
  }

  pendingToken(): string {
    return (sessionStorage.getItem(STORAGE_KEYS.pendingShareToken) ?? '').trim();
  }

  clearPendingToken(): void {
    sessionStorage.removeItem(STORAGE_KEYS.pendingShareToken);
  }

  setSuccess(message: string): void {
    const clean = message.trim();
    if (!clean) return;
    sessionStorage.setItem(STORAGE_KEYS.shareSuccess, clean);
  }

  consumeSuccess(): string {
    const value = (sessionStorage.getItem(STORAGE_KEYS.shareSuccess) ?? '').trim();
    sessionStorage.removeItem(STORAGE_KEYS.shareSuccess);
    return value;
  }
}
