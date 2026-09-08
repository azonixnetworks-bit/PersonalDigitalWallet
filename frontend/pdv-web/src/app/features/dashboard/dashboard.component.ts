import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize, take } from 'rxjs';
import { DashboardResponse, DashboardSubscription } from '../../core/models/dashboard.models';
import { DashboardService } from './dashboard.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DashboardComponent {
  private readonly dashboardService = inject(DashboardService);

  readonly data = signal<DashboardResponse | null>(null);
  readonly loading = signal(true);
  readonly refreshing = signal(false);
  readonly error = signal('');

  readonly greeting = computed(() => {
    const name = this.data()?.user?.fullName?.trim() || 'there';
    const hour = new Date().getHours();
    const welcome = hour < 12 ? 'Good morning' : hour < 17 ? 'Good afternoon' : 'Good evening';
    return `${welcome}, ${name} 👋`;
  });

  readonly isSecure = computed(() => this.data()?.security?.overallStatus === 'SECURE');

  readonly storagePercent = computed(() => {
    const raw = Number(this.data()?.storage?.usagePercent ?? 0);
    if (!Number.isFinite(raw)) return 0;
    return Math.min(100, Math.max(0, raw));
  });

  readonly storagePercentLabel = computed(() => {
    const percent = this.storagePercent();
    return `${percent.toFixed(percent % 1 ? 1 : 0)}% used`;
  });

  readonly storageNearLimit = computed(() => {
    const storage = this.data()?.storage;
    if (!storage) return false;
    return this.storagePercent() >= 80 || storage.currentDocuments >= storage.maxDocuments;
  });

  readonly storageWarning = computed(() => {
    const storage = this.data()?.storage;
    if (!storage || !this.storageNearLimit()) return '';
    return storage.canUpload
      ? 'Your vault is approaching its current plan limit.'
      : 'Your current plan limit has been reached. Upgrade or remove items before uploading more.';
  });

  readonly planMeta = computed(() => {
    const plan = this.data()?.subscription;
    if (!plan) return 'Loading subscription status…';

    if (!plan.isPremium) {
      return '20 documents • 50 MB storage. Upgrade when you need more space.';
    }

    const provider = plan.provider || 'Subscription';
    const status = plan.status || 'ACTIVE';
    let meta = `${provider} • ${status}`;

    if (plan.nextBillingDate) {
      meta += plan.cancelAtPeriodEnd
        ? ` • Access until ${this.formatDate(plan.nextBillingDate)}`
        : ` • Next billing ${this.formatDate(plan.nextBillingDate)}`;
    }

    return meta;
  });

  constructor() {
    this.load();
  }

  reload(): void {
    if (this.loading() || this.refreshing()) return;
    this.load(true);
  }

  formatFileSize(bytes: number | null | undefined): string {
    const value = Math.max(0, Number(bytes) || 0);
    if (value < 1024) return `${value} B`;
    if (value < 1024 ** 2) return `${(value / 1024).toFixed(1)} KB`;
    if (value < 1024 ** 3) return `${(value / (1024 ** 2)).toFixed(1)} MB`;
    if (value < 1024 ** 4) return `${(value / (1024 ** 3)).toFixed(1)} GB`;
    return `${(value / (1024 ** 4)).toFixed(1)} TB`;
  }

  formatDate(value: string | null | undefined): string {
    if (!value) return '';
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return '';

    return new Intl.DateTimeFormat(undefined, {
      month: 'short',
      day: 'numeric',
      year: date.getFullYear() !== new Date().getFullYear() ? 'numeric' : undefined,
      hour: 'numeric',
      minute: '2-digit'
    }).format(date);
  }

  planRoute(url: string | null | undefined): string {
    const normalized = (url || '').toLowerCase();
    if (normalized.includes('stripe-subscription')) return '/stripe-subscription';
    if (normalized.includes('subscription')) return '/subscription';
    return '/subscription';
  }

  primaryPlanText(plan: DashboardSubscription): string {
    return plan.primaryActionText || (plan.isPremium ? 'Manage Billing' : 'Upgrade');
  }

  private load(isRefresh = false): void {
    this.error.set('');
    if (isRefresh) this.refreshing.set(true);
    else this.loading.set(true);

    this.dashboardService.getDashboard().pipe(
      take(1),
      finalize(() => {
        this.loading.set(false);
        this.refreshing.set(false);
      })
    ).subscribe({
      next: response => this.data.set(response),
      error: (error: unknown) => {
        this.error.set(error instanceof Error && error.message
          ? error.message
          : 'Unable to load your dashboard right now.');
      }
    });
  }
}
