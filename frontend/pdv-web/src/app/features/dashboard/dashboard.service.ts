import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiClientService } from '../../core/api/api-client.service';
import { DashboardResponse } from '../../core/models/dashboard.models';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly api = inject(ApiClientService);

  getDashboard(): Observable<DashboardResponse> {
    return this.api.get<DashboardResponse>('/dashboard');
  }
}
