import { Injectable } from '@angular/core';
import { JwtPayload } from '../models/jwt.models';
import { STORAGE_KEYS } from '../storage/storage-keys';

@Injectable({ providedIn: 'root' })
export class TokenService {
  get(): string | null {
    const token = localStorage.getItem(STORAGE_KEYS.accessToken)?.trim();
    return token ? token : null;
  }

  set(token: string): void {
    const normalized = token?.trim();
    if (!normalized) throw new Error('A valid access token is required.');
    localStorage.setItem(STORAGE_KEYS.accessToken, normalized);
  }

  clear(): void { localStorage.removeItem(STORAGE_KEYS.accessToken); }
  has(): boolean { return this.get() !== null; }

  payload(): JwtPayload | null {
    const token = this.get();
    if (!token) return null;
    try {
      const parts = token.split('.');
      if (parts.length !== 3) return null;
      const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/').padEnd(Math.ceil(parts[1].length / 4) * 4, '=');
      const bytes = Uint8Array.from(atob(base64), c => c.charCodeAt(0));
      return JSON.parse(new TextDecoder().decode(bytes)) as JwtPayload;
    } catch { return null; }
  }

  isExpired(): boolean {
    const exp = this.payload()?.exp;
    return typeof exp !== 'number' || exp <= Math.floor(Date.now() / 1000);
  }

  role(): string | null {
    const payload = this.payload();
    if (!payload) return null;
    const role = payload.role ?? payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
    return typeof role === 'string' ? role : null;
  }
}
