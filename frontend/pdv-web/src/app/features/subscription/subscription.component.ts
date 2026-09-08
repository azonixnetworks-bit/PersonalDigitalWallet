import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { catchError, finalize, forkJoin, of, take } from 'rxjs';
import {
  StripeSubscriptionConfigDto,
  SubscriptionDto,
  SubscriptionEntitlementDto,
  SubscriptionMineResponse
} from '../../core/models/subscription.models';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { UiAlertComponent } from '../../shared/components/ui-alert/ui-alert.component';
import { UiLoaderComponent } from '../../shared/components/ui-loader/ui-loader.component';
import { SubscriptionService } from './subscription.service';

@Component({
  selector: 'app-subscription',
  standalone: true,
  imports: [PageHeaderComponent, UiAlertComponent, UiLoaderComponent],
  templateUrl: './subscription.component.html',
  styleUrl: './subscription.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SubscriptionComponent {
  private readonly subscriptionService = inject(SubscriptionService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly config = signal<StripeSubscriptionConfigDto | null>(null);
  readonly subscription = signal<SubscriptionDto | null>(null);
  readonly entitlement = signal<SubscriptionEntitlementDto | null>(null);

  readonly loading = signal(true);
  readonly refreshing = signal(false);
  readonly checkoutBusy = signal(false);
  readonly portalBusy = signal(false);
  readonly verifyingCheckout = signal(false);

  readonly error = signal('');
  readonly info = signal('');
  readonly success = signal('');
  readonly configWarning = signal('');

  readonly isPremium = computed(() =>
    this.entitlement()?.isPremium === true || this.subscription()?.isPremium === true
  );

  readonly status = computed(() => {
    const value = this.subscription()?.status || this.entitlement()?.status || (this.isPremium() ? 'ACTIVE' : 'FREE');
    return String(value).toUpperCase();
  });

  readonly billingProblem = computed(() =>
    ['PAST_DUE', 'UNPAID', 'PAUSED', 'INCOMPLETE'].includes(this.status())
  );

  readonly canManageBilling = computed(() => {
    const subscription = this.subscription();
    return !!subscription && String(subscription.provider || '').toLowerCase() === 'stripe';
  });

  readonly canUpgrade = computed(() =>
    !this.isPremium() && !this.billingProblem() && !!this.config()
  );

  readonly documentPercent = computed(() => {
    const entitlement = this.entitlement();
    if (!entitlement || entitlement.maxDocuments <= 0) return 0;
    return this.percent(entitlement.currentDocuments, entitlement.maxDocuments);
  });

  readonly storagePercent = computed(() => {
    const entitlement = this.entitlement();
    if (!entitlement || entitlement.storageLimitBytes <= 0) return 0;
    return this.percent(entitlement.storageUsedBytes, entitlement.storageLimitBytes);
  });

  constructor() {
    this.handleCheckoutReturn();
  }

  refresh(): void {
    if (this.loading() || this.refreshing() || this.checkoutBusy() || this.portalBusy() || this.verifyingCheckout()) return;
    this.loadBillingData(true);
  }

  startCheckout(): void {
    if (!this.canUpgrade() || this.checkoutBusy()) return;

    this.clearMessages();
    this.checkoutBusy.set(true);

    this.subscriptionService.createCheckoutSession().pipe(
      take(1),
      finalize(() => this.checkoutBusy.set(false))
    ).subscribe({
      next: response => {
        if (!this.isTrustedStripeUrl(response?.checkoutUrl)) {
          this.error.set('Stripe Checkout URL was not returned or could not be validated.');
          return;
        }

        window.location.assign(response.checkoutUrl);
      },
      error: error => this.error.set(this.apiMessage(error, 'Stripe Checkout could not be started.'))
    });
  }

  openBillingPortal(): void {
    if (!this.canManageBilling() || this.portalBusy()) return;

    this.clearMessages();
    this.portalBusy.set(true);

    this.subscriptionService.createPortalSession().pipe(
      take(1),
      finalize(() => this.portalBusy.set(false))
    ).subscribe({
      next: response => {
        if (!this.isTrustedStripeUrl(response?.portalUrl)) {
          this.error.set('Stripe billing portal URL was not returned or could not be validated.');
          return;
        }

        window.location.assign(response.portalUrl);
      },
      error: error => this.error.set(this.apiMessage(error, 'Stripe Customer Portal could not be opened.'))
    });
  }

  formatMoney(config: StripeSubscriptionConfigDto | null): string {
    if (!config?.currency || config.unitAmount === null || config.unitAmount === undefined) {
      return 'Price verified by Stripe';
    }

    const currency = String(config.currency).toUpperCase();
    const amount = Number(config.unitAmount);
    if (!Number.isFinite(amount)) return 'Price verified by Stripe';

    try {
      const formatter = new Intl.NumberFormat(undefined, { style: 'currency', currency });
     const intlDigits =
  formatter.resolvedOptions().maximumFractionDigits ?? 2;

const stripeDigits: number =
  ['ISK', 'UGX'].includes(currency) ? 2 : intlDigits;
      return formatter.format(amount / (10 ** stripeDigits));
    }
    catch {
      return `${currency} ${amount}`;
    }
  }

  billingInterval(config: StripeSubscriptionConfigDto | null): string {
    const interval = String(config?.billingInterval || '').trim().toLowerCase();
    const count = Number(config?.billingIntervalCount || 1);
    if (!interval) return 'Recurring billing';
    return count > 1 ? `Every ${count} ${interval}s` : `Every ${interval}`;
  }

  formatBytes(value: number | null | undefined): string {
    const bytes = Number(value || 0);
    if (!Number.isFinite(bytes) || bytes <= 0) return '0 B';

    const units = ['B', 'KB', 'MB', 'GB', 'TB'];
    const index = Math.min(Math.floor(Math.log(bytes) / Math.log(1024)), units.length - 1);
    const amount = bytes / (1024 ** index);
    return `${amount >= 10 || index === 0 ? amount.toFixed(0) : amount.toFixed(1)} ${units[index]}`;
  }

  formatDate(value: string | null | undefined): string {
    if (!value) return 'â€”';
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return 'â€”';
    return new Intl.DateTimeFormat(undefined, {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    }).format(date);
  }

  statusClass(): string {
    const status = this.status();
    if (['ACTIVE', 'TRIALING'].includes(status)) return 'status-active';
    if (['PAST_DUE', 'UNPAID', 'PAUSED', 'INCOMPLETE'].includes(status)) return 'status-warning';
    if (['CANCELED', 'INCOMPLETE_EXPIRED'].includes(status)) return 'status-cancelled';
    return 'status-neutral';
  }

  private handleCheckoutReturn(): void {
    const checkout = this.route.snapshot.queryParamMap.get('checkout');
    const sessionId = this.route.snapshot.queryParamMap.get('session_id');

    if (checkout === 'cancelled') {
      this.info.set('Stripe Checkout was cancelled. No Premium access was activated.');
      void this.clearCheckoutQuery();
      this.loadBillingData();
      return;
    }

    if (checkout === 'success' && sessionId) {
      this.verifyingCheckout.set(true);
      this.info.set('Payment completed. Verifying your subscription securely with the PDV backendâ€¦');

      this.subscriptionService.verifyCheckout(sessionId).pipe(
        take(1),
        finalize(() => this.verifyingCheckout.set(false))
      ).subscribe({
        next: response => {
          this.success.set(response?.message || 'Premium subscription verified successfully.');
          this.info.set('');
          void this.clearCheckoutQuery();
          this.loadBillingData();
        },
        error: error => {
          this.info.set(this.apiMessage(
            error,
            'Payment completed. Subscription synchronization may still finish through the signed Stripe webhook.'
          ));
          void this.clearCheckoutQuery();
          this.loadBillingData();
        }
      });
      return;
    }

    if (checkout === 'success') {
      this.info.set('Stripe returned successfully, but no Checkout Session ID was available for immediate verification.');
      void this.clearCheckoutQuery();
    }

    this.loadBillingData();
  }

  private loadBillingData(isRefresh = false): void {
    this.error.set('');
    this.configWarning.set('');
    if (isRefresh) this.refreshing.set(true);
    else this.loading.set(true);

    forkJoin({
      config: this.subscriptionService.getStripeConfig().pipe(
        catchError(error => {
          this.configWarning.set(this.apiMessage(error, 'Stripe billing configuration is temporarily unavailable.'));
          return of<StripeSubscriptionConfigDto | null>(null);
        })
      ),
      mine: this.subscriptionService.getMine().pipe(
        catchError(error => {
          this.error.set(this.apiMessage(error, 'Current Stripe subscription could not be loaded.'));
          return of<SubscriptionMineResponse | null>(null);
        })
      ),
      entitlement: this.subscriptionService.getEntitlements().pipe(
        catchError(error => {
          if (!this.error()) {
            this.error.set(this.apiMessage(error, 'Current plan limits could not be loaded.'));
          }
          return of<SubscriptionEntitlementDto | null>(null);
        })
      )
    }).pipe(
      take(1),
      finalize(() => {
        this.loading.set(false);
        this.refreshing.set(false);
      })
    ).subscribe(result => {
      this.config.set(result.config);
      this.subscription.set(result.mine?.subscription ?? null);
      this.entitlement.set(result.entitlement);

      if (!result.mine && !result.entitlement) {
        this.error.set(this.error() || 'Billing information could not be loaded. Please try again.');
      }
    });
  }

  private clearMessages(): void {
    this.error.set('');
    this.info.set('');
    this.success.set('');
  }

  private percent(current: number, total: number): number {
    if (total <= 0) return 0;
    return Math.max(0, Math.min(100, Math.round((current / total) * 100)));
  }

  private isTrustedStripeUrl(value: unknown): value is string {
    if (typeof value !== 'string' || !value.trim()) return false;

    try {
      const url = new URL(value);
      const host = url.hostname.toLowerCase();
      return url.protocol === 'https:' && (host === 'stripe.com' || host.endsWith('.stripe.com'));
    }
    catch {
      return false;
    }
  }

  private clearCheckoutQuery(): Promise<boolean> {
    return this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {},
      replaceUrl: true
    });
  }

  private apiMessage(error: unknown, fallback: string): string {
    if (error instanceof HttpErrorResponse) {
      const body = (error as HttpErrorResponse).error as { message?: unknown; title?: unknown } | null;
      if (typeof body?.message === 'string' && body.message.trim()) return body.message;
      if (typeof body?.title === 'string' && body.title.trim()) return body.title;
    }

    return error instanceof Error && error.message ? error.message : fallback;
  }
}
