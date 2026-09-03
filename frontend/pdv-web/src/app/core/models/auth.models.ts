export interface RegisterRequest { fullName: string; email: string; password: string; }
export interface RegisterResponse { email: string; requiresEmailVerification: boolean; message: string; }
export interface VerifyEmailRequest { email: string; otp: string; }
export interface VerifyEmailResponse { emailVerified: boolean; setupToken: string; message: string; }
export interface ResendEmailOtpRequest { email: string; }
export interface MessageResponse { message: string; }
export interface ForgotPasswordRequest { email: string; }
export interface ResetPasswordRequest { token: string; newPassword: string; confirmPassword: string; }
export interface SetupTotpRequest { setupToken: string; }
export interface TotpSetupResponse { secretKey: string; otpAuthUri: string; qrCodeDataUrl: string; }
export interface VerifyTotpSetupRequest { setupToken: string; code: string; }
export interface LoginRequest { email: string; password: string; }
export interface LoginResponse {
  requiresTotp: boolean;
  challengeToken?: string | null;
  userId?: number | null;
  fullName?: string | null;
  email?: string | null;
  role?: string | null;
  token?: string | null;
  message: string;
}
export interface VerifyLoginTotpRequest { challengeToken: string; code: string; }
export interface FinalLoginResponse { userId: number; fullName: string; email: string; role: string; token: string; }
