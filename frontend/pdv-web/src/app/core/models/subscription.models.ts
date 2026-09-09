export interface StripeSubscriptionConfigDto {
  provider: string;
  planName: string;
  environment: string;
  currency: string;
  unitAmount: number | null;
  billingInterval: string;
  billingIntervalCount: number;
}

export interface SubscriptionDto {
  id: number;
  provider: string;
  payPalSubscriptionId: string | null;
  payPalPlanId: string | null;
  stripeSubscriptionId: string | null;
  stripePriceId: string | null;
  planName: string;
  status: string;
  isPremium: boolean;
  cancelAtPeriodEnd: boolean;
  startDate: string | null;
  nextBillingDate: string | null;
  cancelledAt: string | null;
  lastVerifiedAt: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface SubscriptionMineResponse {
  hasSubscription: boolean;
  subscription: SubscriptionDto | null;
}

export interface SubscriptionEntitlementDto {
  planName: string;
  isPremium: boolean;
  maxDocuments: number;
  currentDocuments: number;
  storageLimitBytes: number;
  storageUsedBytes: number;
  storageRemainingBytes: number;
  maxUploadBytes: number;
  canUpload: boolean;
  provider: string | null;
  status: string | null;
  nextBillingDate: string | null;
  cancelAtPeriodEnd: boolean;
}

export interface CreateCheckoutSessionResponseDto {
  checkoutUrl: string;
}

export interface CreatePortalSessionResponseDto {
  portalUrl: string;
}

export interface VerifyCheckoutResponseDto {
  message?: string;
  subscription?: SubscriptionDto | null;
}
