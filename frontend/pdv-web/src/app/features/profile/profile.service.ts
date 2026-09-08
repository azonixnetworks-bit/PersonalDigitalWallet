import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiClientService } from '../../core/api/api-client.service';
import { ProfileDto, UpdateProfileRequest } from '../../core/models/profile.models';

@Injectable({ providedIn: 'root' })
export class ProfileService {
  private readonly api = inject(ApiClientService);

  getProfile(): Observable<ProfileDto> {
    return this.api.get<ProfileDto>('/profile');
  }

  updateProfile(request: UpdateProfileRequest): Observable<ProfileDto> {
    return this.api.put<ProfileDto>('/profile', request);
  }
}
