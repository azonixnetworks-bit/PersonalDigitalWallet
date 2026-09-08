import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiClientService } from '../../core/api/api-client.service';
import {
  CreateCheckoutSessionResponseDto,
  CreatePortalSessionResponseDto,
  StripeSubscriptionConfigDto,
  SubscriptionEntitlementDto,
  SubscriptionMineResponse,
  VerifyCheckoutResponseDto
} from '../../core/models/subscription.models';

@Injectable({ providedIn: 'root' })
export class SubscriptionService {
  private readonly api = inject(ApiClientService);

  getStripeConfig(): Observable<StripeSubscriptionConfigDto> {
    return this.api.get<StripeSubscriptionConfigDto>('/stripe/subscriptions/config');
  }

  getMine(): Observable<SubscriptionMineResponse> {
    return this.api.get<SubscriptionMineResponse>('/stripe/subscriptions/me');
  }

  getEntitlements(): Observable<SubscriptionEntitlementDto> {
    return this.api.get<SubscriptionEntitlementDto>('/subscriptions/entitlements');
  }

  createCheckoutSession(): Observable<CreateCheckoutSessionResponseDto> {
    return this.api.post<CreateCheckoutSessionResponseDto>('/stripe/subscriptions/checkout-session');
  }

  verifyCheckout(sessionId: string): Observable<VerifyCheckoutResponseDto> {
    return this.api.post<VerifyCheckoutResponseDto>('/stripe/subscriptions/verify-checkout', { sessionId });
  }

  createPortalSession(): Observable<CreatePortalSessionResponseDto> {
    return this.api.post<CreatePortalSessionResponseDto>('/stripe/subscriptions/portal-session');
  }
}
