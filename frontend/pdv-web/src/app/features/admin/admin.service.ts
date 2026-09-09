import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiClientService } from '../../core/api/api-client.service';
import {
  AdminDashboardDto,
  AdminSubscriptionDto,
  AdminSubscriptionSummaryDto,
  AdminUserDto,
  MessageResponseDto,
  UpdateAdminUserStatusRequest
} from '../../core/models/admin.models';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly api = inject(ApiClientService);

  getDashboard(): Observable<AdminDashboardDto> {
    return this.api.get<AdminDashboardDto>('/admin/dashboard');
  }

  getUsers(): Observable<AdminUserDto[]> {
    return this.api.get<AdminUserDto[]>('/admin/users');
  }

  updateUserStatus(userId: number, isActive: boolean): Observable<MessageResponseDto> {
    const body: UpdateAdminUserStatusRequest = { isActive };
    return this.api.put<MessageResponseDto>(`/admin/users/${userId}/status`, body);
  }

  getSubscriptionSummary(): Observable<AdminSubscriptionSummaryDto> {
    return this.api.get<AdminSubscriptionSummaryDto>('/admin/subscriptions/summary');
  }

  getSubscriptions(): Observable<AdminSubscriptionDto[]> {
    return this.api.get<AdminSubscriptionDto[]>('/admin/subscriptions');
  }
}
