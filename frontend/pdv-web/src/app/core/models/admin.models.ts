export interface AdminUploadDto {
  uploadedBy: string;
  fileName: string;
  fileSize: number;
  uploadedAt: string;
}

export interface AdminDashboardDto {
  totalUsers: number;
  totalUploads: number;
  totalStoredFiles: number;
  recentUploads: AdminUploadDto[];
}

export interface AdminUserDto {
  id: number;
  fullName: string;
  email: string;
  role: string;
  isActive: boolean;
}

export interface UpdateAdminUserStatusRequest {
  isActive: boolean;
}

export interface MessageResponseDto {
  message?: string;
}

export interface AdminSubscriptionSummaryDto {
  totalUsers: number;
  premiumUsers: number;
  freeUsers: number;
  totalSubscriptionRecords: number;
  activeSubscriptions: number;
  cancelledSubscriptions: number;
  suspendedSubscriptions: number;
  expiredSubscriptions: number;
}

export interface AdminSubscriptionDto {
  userId: number;
  userName: string;
  maskedEmail: string;
  provider: string;
  planName: string;
  status: string;
  isPremium: boolean;
  payPalSubscriptionReference: string;
  stripeSubscriptionReference: string;
  cancelAtPeriodEnd: boolean;
  startDate: string | null;
  nextBillingDate: string | null;
  cancelledAt: string | null;
  lastVerifiedAt: string | null;
  createdAt: string;
  updatedAt: string;
}
