import { Injectable } from '@angular/core';
import { TotpSetupResponse } from '../models/auth.models';
import { STORAGE_KEYS } from '../storage/storage-keys';

@Injectable({ providedIn: 'root' })
export class AuthFlowStorageService {
  beginRegistration(email: string): void {
    const normalized = this.normalizeEmail(email);
    if (!normalized) {
      throw new Error('A valid registration email is required.');
    }

    sessionStorage.setItem(STORAGE_KEYS.registrationEmail, normalized);
    sessionStorage.removeItem(STORAGE_KEYS.totpSetupToken);
    sessionStorage.removeItem(STORAGE_KEYS.totpSetupResponse);
  }

  registrationEmail(): string {
    return this.normalizeEmail(sessionStorage.getItem(STORAGE_KEYS.registrationEmail) ?? '');
  }

  setTotpSetupToken(token: string): void {
    const normalized = token.trim();
    if (!normalized) {
      throw new Error('A valid authenticator setup token is required.');
    }
    sessionStorage.setItem(STORAGE_KEYS.totpSetupToken, normalized);
  }

  totpSetupToken(): string {
    return (sessionStorage.getItem(STORAGE_KEYS.totpSetupToken) ?? '').trim();
  }

  setTotpSetupResponse(response: TotpSetupResponse): void {
    if (!this.isValidTotpSetup(response)) {
      throw new Error('Authenticator setup information is invalid.');
    }

    sessionStorage.setItem(
      STORAGE_KEYS.totpSetupResponse,
      JSON.stringify({
        secretKey: response.secretKey.trim(),
        otpAuthUri: response.otpAuthUri?.trim() ?? '',
        qrCodeDataUrl: response.qrCodeDataUrl.trim()
      })
    );
  }

  totpSetupResponse(): TotpSetupResponse | null {
    const value = sessionStorage.getItem(STORAGE_KEYS.totpSetupResponse);
    if (!value) return null;

    try {
      const parsed = JSON.parse(value) as Partial<TotpSetupResponse>;
      if (!this.isValidTotpSetup(parsed)) {
        sessionStorage.removeItem(STORAGE_KEYS.totpSetupResponse);
        return null;
      }

      return {
        secretKey: parsed.secretKey.trim(),
        otpAuthUri: parsed.otpAuthUri?.trim() ?? '',
        qrCodeDataUrl: parsed.qrCodeDataUrl.trim()
      };
    } catch {
      sessionStorage.removeItem(STORAGE_KEYS.totpSetupResponse);
      return null;
    }
  }

  clearTotpSetupResponse(): void {
    sessionStorage.removeItem(STORAGE_KEYS.totpSetupResponse);
  }

  clearRegistrationFlow(): void {
    sessionStorage.removeItem(STORAGE_KEYS.registrationEmail);
    sessionStorage.removeItem(STORAGE_KEYS.totpSetupToken);
    sessionStorage.removeItem(STORAGE_KEYS.totpSetupResponse);
  }

  setLoginChallenge(challengeToken: string, email: string): void {
    const token = challengeToken.trim();
    if (!token) {
      throw new Error('A valid login verification challenge is required.');
    }

    sessionStorage.setItem(STORAGE_KEYS.loginMfaChallenge, token);

    const normalizedEmail = this.normalizeEmail(email);
    if (normalizedEmail) {
      sessionStorage.setItem(STORAGE_KEYS.loginEmail, normalizedEmail);
    } else {
      sessionStorage.removeItem(STORAGE_KEYS.loginEmail);
    }
  }

  loginChallenge(): string {
    return (sessionStorage.getItem(STORAGE_KEYS.loginMfaChallenge) ?? '').trim();
  }

  loginEmail(): string {
    return this.normalizeEmail(sessionStorage.getItem(STORAGE_KEYS.loginEmail) ?? '');
  }

  clearLoginChallenge(): void {
    sessionStorage.removeItem(STORAGE_KEYS.loginMfaChallenge);
    sessionStorage.removeItem(STORAGE_KEYS.loginEmail);
  }

  setPostLoginRedirect(path: string): void {
    if (!this.isSafeLocalPath(path)) {
      sessionStorage.removeItem(STORAGE_KEYS.postLoginRedirect);
      return;
    }
    sessionStorage.setItem(STORAGE_KEYS.postLoginRedirect, path);
  }

  consumePostLoginRedirect(fallback = '/dashboard'): string {
    const value = sessionStorage.getItem(STORAGE_KEYS.postLoginRedirect);
    sessionStorage.removeItem(STORAGE_KEYS.postLoginRedirect);
    return this.isSafeLocalPath(value) ? value : fallback;
  }

  clearAll(): void {
    this.clearRegistrationFlow();
    this.clearLoginChallenge();
    sessionStorage.removeItem(STORAGE_KEYS.postLoginRedirect);
  }

  private normalizeEmail(value: string): string {
    return value.trim().toLowerCase();
  }

  private isSafeLocalPath(value: string | null): value is string {
    return !!value
      && value.startsWith('/')
      && !value.startsWith('//')
      && !value.includes('\\');
  }

  private isValidTotpSetup(value: Partial<TotpSetupResponse>): value is TotpSetupResponse {
    return typeof value.secretKey === 'string'
      && value.secretKey.trim().length > 0
      && typeof value.qrCodeDataUrl === 'string'
      && value.qrCodeDataUrl.trim().length > 0
      && (value.otpAuthUri === undefined || typeof value.otpAuthUri === 'string');
  }
}
