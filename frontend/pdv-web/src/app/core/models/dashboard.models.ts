export interface DashboardResponse {
  user: DashboardUser;
  summary: DashboardSummary;
  storage: DashboardStorage;
  subscription: DashboardSubscription;
  security: DashboardSecurity;
  recentDocuments: DashboardRecentDocument[];
  recentFolders: DashboardRecentFolder[];
}

export interface DashboardUser {
  fullName: string;
}

export interface DashboardSummary {
  folderCount: number;
  documentCount: number;
  credentialCount: number;
  sharedWithMeCount: number;
}

export interface DashboardStorage {
  usedBytes: number;
  limitBytes: number;
  remainingBytes: number;
  usagePercent: number;
  currentDocuments: number;
  maxDocuments: number;
  canUpload: boolean;
}

export interface DashboardSubscription {
  planName: string;
  isPremium: boolean;
  provider?: string | null;
  status?: string | null;
  nextBillingDate?: string | null;
  cancelAtPeriodEnd: boolean;
  primaryActionUrl: string;
  primaryActionText: string;
  secondaryActionUrl?: string | null;
  secondaryActionText?: string | null;
}

export interface DashboardSecurity {
  emailVerified: boolean;
  totpEnabled: boolean;
  encryptedStorageActive: boolean;
  overallStatus: 'SECURE' | 'ACTION_REQUIRED' | string;
}

export interface DashboardRecentDocument {
  id: number;
  fileName: string;
  folderName: string;
  fileSize: number;
  createdAt: string;
}

export interface DashboardRecentFolder {
  id: number;
  name: string;
  documentCount: number;
  createdAt: string;
}
