export interface ProfileDto {
  id: number;
  fullName: string;
  email: string;
  role: string;
  isActive: boolean;
  isEmailVerified: boolean;
  isTotpEnabled: boolean;
  createdAt: string;
}

export interface UpdateProfileRequest {
  fullName: string;
}
