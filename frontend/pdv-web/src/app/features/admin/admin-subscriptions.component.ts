import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal
} from '@angular/core';
import { finalize, forkJoin, take } from 'rxjs';
import {
  AdminSubscriptionDto,
  AdminSubscriptionSummaryDto
} from '../../core/models/admin.models';
import { AdminService } from './admin.service';

@Component({
  selector: 'app-admin-subscriptions',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './admin-subscriptions.component.html',
  styleUrl: './admin-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminSubscriptionsComponent implements OnInit {
  private readonly adminService = inject(AdminService);

  readonly loading = signal(true);
  readonly summary = signal<AdminSubscriptionSummaryDto | null>(null);
  readonly subscriptions = signal<AdminSubscriptionDto[]>([]);
  readonly query = signal('');
  readonly error = signal('');

  readonly filteredSubscriptions = computed(() => {
    const keyword = this.query().trim().toLowerCase();
    const subscriptions = this.subscriptions();

    if (!keyword) return subscriptions;

    return subscriptions.filter(subscription => {
      const searchable = [
        subscription.userName,
        subscription.maskedEmail,
        subscription.provider,
        subscription.planName,
        subscription.status,
        this.providerReference(subscription)
      ].join(' ').toLowerCase();

      return searchable.includes(keyword);
    });
  });

  ngOnInit(): void {
    this.loadBillingData();
  }

  loadBillingData(): void {
    this.loading.set(true);
    this.error.set('');

    forkJoin({
      summary: this.adminService.getSubscriptionSummary(),
      subscriptions: this.adminService.getSubscriptions()
    }).pipe(
      take(1),
      finalize(() => this.loading.set(false))
    ).subscribe({
      next: result => {
        this.summary.set(this.normalizeSummary(result.summary));
        this.subscriptions.set(this.normalizeSubscriptions(result.subscriptions));
      },
      error: error => {
        this.error.set(this.errorMessage(error, 'Billing metadata could not be loaded.'));
      }
    });
  }

  onSearch(event: Event): void {
    const input = event.target as HTMLInputElement | null;
    this.query.set(input?.value ?? '');
  }

  providerReference(subscription: AdminSubscriptionDto): string {
    const provider = subscription.provider.trim().toLowerCase();

    if (provider === 'stripe') {
      return subscription.stripeSubscriptionReference || '-';
    }

    if (provider === 'paypal') {
      return subscription.payPalSubscriptionReference || '-';
    }

    return subscription.stripeSubscriptionReference
      || subscription.payPalSubscriptionReference
      || '-';
  }

  statusClass(status: string | null | undefined): string {
    const value = String(status || '').trim().toUpperCase();

    if (['ACTIVE', 'TRIALING'].includes(value)) return 'badge-active';
    if (['CANCELED', 'CANCELLED', 'EXPIRED', 'INCOMPLETE_EXPIRED'].includes(value)) return 'badge-disabled';
    if (['SUSPENDED', 'PAST_DUE', 'UNPAID', 'PAUSED', 'INCOMPLETE'].includes(value)) return 'badge-warning';
    return 'badge-neutral';
  }

  formatDate(value: string | null | undefined): string {
    if (!value) return '-';
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return '-';

    return new Intl.DateTimeFormat(undefined, {
      year: 'numeric',
      month: 'short',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit'
    }).format(date);
  }

  private normalizeSummary(value: AdminSubscriptionSummaryDto | null | undefined): AdminSubscriptionSummaryDto {
    return {
      totalUsers: this.safeCount(value?.totalUsers),
      premiumUsers: this.safeCount(value?.premiumUsers),
      freeUsers: this.safeCount(value?.freeUsers),
      totalSubscriptionRecords: this.safeCount(value?.totalSubscriptionRecords),
      activeSubscriptions: this.safeCount(value?.activeSubscriptions),
      cancelledSubscriptions: this.safeCount(value?.cancelledSubscriptions),
      suspendedSubscriptions: this.safeCount(value?.suspendedSubscriptions),
      expiredSubscriptions: this.safeCount(value?.expiredSubscriptions)
    };
  }

  private normalizeSubscriptions(value: AdminSubscriptionDto[] | null | undefined): AdminSubscriptionDto[] {
    if (!Array.isArray(value)) return [];

    return value
      .map(subscription => ({
        userId: Number(subscription?.userId),
        userName: String(subscription?.userName ?? '').trim(),
        maskedEmail: String(subscription?.maskedEmail ?? '').trim(),
        provider: String(subscription?.provider ?? '').trim(),
        planName: String(subscription?.planName ?? '').trim(),
        status: String(subscription?.status ?? '').trim() || 'UNKNOWN',
        isPremium: Boolean(subscription?.isPremium),
        payPalSubscriptionReference: String(subscription?.payPalSubscriptionReference ?? '').trim(),
        stripeSubscriptionReference: String(subscription?.stripeSubscriptionReference ?? '').trim(),
        cancelAtPeriodEnd: Boolean(subscription?.cancelAtPeriodEnd),
        startDate: subscription?.startDate || null,
        nextBillingDate: subscription?.nextBillingDate || null,
        cancelledAt: subscription?.cancelledAt || null,
        lastVerifiedAt: subscription?.lastVerifiedAt || null,
        createdAt: String(subscription?.createdAt ?? ''),
        updatedAt: String(subscription?.updatedAt ?? '')
      }))
      .filter(subscription => Number.isInteger(subscription.userId) && subscription.userId > 0);
  }

  private safeCount(value: number | null | undefined): number {
    const count = Number(value ?? 0);
    return Number.isFinite(count) ? Math.max(0, Math.trunc(count)) : 0;
  }

  private errorMessage(error: unknown, fallback: string): string {
    if (error instanceof HttpErrorResponse) {
      const message = error.error?.message;
      if (typeof message === 'string' && message.trim()) return message.trim();
      if (error.status === 403) return 'Administrator access is required to view billing metadata.';
      if (error.status === 401) return 'Your administrator session is no longer authorized.';
    }

    return fallback;
  }
}
