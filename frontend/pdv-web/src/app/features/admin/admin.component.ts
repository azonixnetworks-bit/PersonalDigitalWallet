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
  AdminDashboardDto,
  AdminUploadDto,
  AdminUserDto
} from '../../core/models/admin.models';
import { AdminService } from './admin.service';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './admin.component.html',
  styleUrl: './admin-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminComponent implements OnInit {
  private readonly adminService = inject(AdminService);

  readonly loading = signal(true);
  readonly refreshingUsers = signal(false);
  readonly pendingUserId = signal<number | null>(null);
  readonly dashboard = signal<AdminDashboardDto | null>(null);
  readonly users = signal<AdminUserDto[]>([]);
  readonly query = signal('');
  readonly error = signal('');
  readonly info = signal('');

  readonly filteredUsers = computed(() => {
    const keyword = this.query().trim().toLowerCase();
    const users = this.users();

    if (!keyword) return users;

    return users.filter(user =>
      user.fullName.toLowerCase().includes(keyword)
      || user.email.toLowerCase().includes(keyword)
      || user.role.toLowerCase().includes(keyword)
    );
  });

  ngOnInit(): void {
    this.loadAll();
  }

  loadAll(): void {
    this.loading.set(true);
    this.error.set('');
    this.info.set('');

    forkJoin({
      dashboard: this.adminService.getDashboard(),
      users: this.adminService.getUsers()
    }).pipe(
      take(1),
      finalize(() => this.loading.set(false))
    ).subscribe({
      next: result => {
        this.dashboard.set(this.normalizeDashboard(result.dashboard));
        this.users.set(this.normalizeUsers(result.users));
      },
      error: error => {
        this.error.set(this.errorMessage(error, 'Administrator dashboard could not be loaded.'));
      }
    });
  }

  onSearch(event: Event): void {
    const input = event.target as HTMLInputElement | null;
    this.query.set(input?.value ?? '');
  }

  toggleUserStatus(user: AdminUserDto): void {
    if (this.isAdminRole(user.role) || this.pendingUserId() !== null) return;

    const nextStatus = !user.isActive;
    const action = nextStatus ? 'Enable' : 'Disable';
    const confirmed = window.confirm(`${action} "${user.email}"?`);

    if (!confirmed) return;

    this.pendingUserId.set(user.id);
    this.error.set('');
    this.info.set('');

    this.adminService.updateUserStatus(user.id, nextStatus).pipe(
      take(1),
      finalize(() => this.pendingUserId.set(null))
    ).subscribe({
      next: response => {
        this.info.set(response?.message || `User ${nextStatus ? 'enabled' : 'disabled'} successfully.`);
        this.refreshUsers();
      },
      error: error => {
        this.error.set(this.errorMessage(error, 'User account status could not be changed.'));
      }
    });
  }

  isAdminRole(role: string | null | undefined): boolean {
    return String(role || '').trim().toLowerCase() === 'admin';
  }

  formatBytes(value: number | null | undefined): string {
    const bytes = Number(value ?? 0);
    if (!Number.isFinite(bytes) || bytes <= 0) return '0 B';

    const units = ['B', 'KB', 'MB', 'GB', 'TB'];
    const index = Math.min(Math.floor(Math.log(bytes) / Math.log(1024)), units.length - 1);
    const amount = bytes / (1024 ** index);
    const digits = amount >= 10 || index === 0 ? 0 : 1;
    return `${amount.toFixed(digits)} ${units[index]}`;
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

  private refreshUsers(): void {
    this.refreshingUsers.set(true);

    this.adminService.getUsers().pipe(
      take(1),
      finalize(() => this.refreshingUsers.set(false))
    ).subscribe({
      next: users => this.users.set(this.normalizeUsers(users)),
      error: error => {
        this.error.set(this.errorMessage(error, 'User list could not be refreshed.'));
      }
    });
  }

  private normalizeDashboard(value: AdminDashboardDto | null | undefined): AdminDashboardDto {
    return {
      totalUsers: this.safeCount(value?.totalUsers),
      totalUploads: this.safeCount(value?.totalUploads),
      totalStoredFiles: this.safeCount(value?.totalStoredFiles),
      recentUploads: Array.isArray(value?.recentUploads)
        ? value.recentUploads.map(upload => this.normalizeUpload(upload))
        : []
    };
  }

  private normalizeUpload(upload: AdminUploadDto): AdminUploadDto {
    return {
      uploadedBy: String(upload?.uploadedBy ?? '').trim() || 'Unknown User',
      fileName: String(upload?.fileName ?? '').trim() || 'Unnamed file',
      fileSize: Math.max(0, Number(upload?.fileSize ?? 0) || 0),
      uploadedAt: String(upload?.uploadedAt ?? '')
    };
  }

  private normalizeUsers(value: AdminUserDto[] | null | undefined): AdminUserDto[] {
    if (!Array.isArray(value)) return [];

    return value
      .map(user => ({
        id: Number(user?.id),
        fullName: String(user?.fullName ?? '').trim(),
        email: String(user?.email ?? '').trim(),
        role: String(user?.role ?? 'User').trim() || 'User',
        isActive: Boolean(user?.isActive)
      }))
      .filter(user => Number.isInteger(user.id) && user.id > 0);
  }

  private safeCount(value: number | null | undefined): number {
    const count = Number(value ?? 0);
    return Number.isFinite(count) ? Math.max(0, Math.trunc(count)) : 0;
  }

  private errorMessage(error: unknown, fallback: string): string {
    if (error instanceof HttpErrorResponse) {
      const message = error.error?.message;
      if (typeof message === 'string' && message.trim()) return message.trim();
      if (error.status === 403) return 'Administrator access is required for this action.';
      if (error.status === 401) return 'Your administrator session is no longer authorized.';
    }

    return fallback;
  }
}
