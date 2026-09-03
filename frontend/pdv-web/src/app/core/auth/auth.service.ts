import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiClientService } from '../api/api-client.service';
import {
  FinalLoginResponse,
  ForgotPasswordRequest,
  LoginRequest,
  LoginResponse,
  MessageResponse,
  RegisterRequest,
  RegisterResponse,
  ResendEmailOtpRequest,
  ResetPasswordRequest,
  SetupTotpRequest,
  TotpSetupResponse,
  VerifyEmailRequest,
  VerifyEmailResponse,
  VerifyLoginTotpRequest,
  VerifyTotpSetupRequest
} from '../models/auth.models';
import { AuthFlowStorageService } from './auth-flow-storage.service';
import { TokenService } from './token.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  constructor(
    private readonly api: ApiClientService,
    private readonly tokens: TokenService,
    private readonly authFlow: AuthFlowStorageService
  ) {}

  register(request: RegisterRequest): Observable<RegisterResponse> {
    return this.api.post('auth/register', request);
  }

  verifyEmail(request: VerifyEmailRequest): Observable<VerifyEmailResponse> {
    return this.api.post('auth/verify-email', request);
  }

  resendEmailOtp(request: ResendEmailOtpRequest): Observable<MessageResponse> {
    return this.api.post('auth/resend-email-otp', request);
  }

  forgotPassword(request: ForgotPasswordRequest): Observable<MessageResponse> {
    return this.api.post('auth/forgot-password', request);
  }

  resetPassword(request: ResetPasswordRequest): Observable<MessageResponse> {
    return this.api.post('auth/reset-password', request);
  }

  setupTotp(request: SetupTotpRequest): Observable<TotpSetupResponse> {
    return this.api.post('auth/setup-totp', request);
  }

  verifyTotpSetup(request: VerifyTotpSetupRequest): Observable<MessageResponse> {
    return this.api.post('auth/verify-totp-setup', request);
  }

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.api.post('auth/login', request);
  }

  verifyLoginTotp(request: VerifyLoginTotpRequest): Observable<FinalLoginResponse> {
    return this.api.post('auth/verify-login-totp', request);
  }

  logout(): Observable<void> {
    return this.api.post<void>('auth/logout');
  }

  clearLocalSession(): void {
    this.tokens.clear();
    this.authFlow.clearAll();

    // Legacy Phase 0/1 values are removed during migration as well.
    sessionStorage.removeItem('pdv_role');
    sessionStorage.removeItem('pdv_user');
  }
}
