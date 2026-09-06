import { ChangeDetectionStrategy, Component, OnInit, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthFlowStorageService } from '../../core/auth/auth-flow-storage.service';
import { TokenService } from '../../core/auth/token.service';
import { AppBrandComponent } from '../../shared/components/app-brand/app-brand.component';
import { UiLoaderComponent } from '../../shared/components/ui-loader/ui-loader.component';
import { ShareInvitationStorageService } from './share-invitation-storage.service';
import { SharingService } from './sharing.service';

@Component({
  selector: 'app-share-invitation',
  standalone: true,
  imports: [RouterLink, AppBrandComponent, UiLoaderComponent],
  templateUrl: './share-invitation.component.html',
  styleUrl: './share-invitation.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ShareInvitationComponent implements OnInit {
  readonly processing = signal(true);
  readonly status = signal('Checking secure invitation…');
  readonly failed = signal(false);

  constructor(
    private readonly tokens: TokenService,
    private readonly authFlow: AuthFlowStorageService,
    private readonly invitationStorage: ShareInvitationStorageService,
    private readonly sharingApi: SharingService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.captureTokenFromFragment();
    const token = this.invitationStorage.pendingToken();

    if (!token) {
      this.processing.set(false);
      this.status.set('This secure invitation is missing or is no longer available.');
      return;
    }

    if (!this.tokens.has() || this.tokens.isExpired()) {
      this.tokens.clear();
      this.authFlow.setPostLoginRedirect('/share-invitation');
      void this.router.navigate(['/login'], {
        queryParams: { reason: 'share' },
        replaceUrl: true
      });
      return;
    }

    if ((this.tokens.role() || '').toLowerCase() !== 'user') {
      this.processing.set(false);
      this.failed.set(true);
      this.status.set('This invitation requires the registered recipient user account.');
      return;
    }

    this.accept(token);
  }

  loginWithCorrectAccount(): void {
    this.tokens.clear();
    this.authFlow.clearLoginChallenge();
    this.authFlow.setPostLoginRedirect('/share-invitation');
    void this.router.navigate(['/login'], {
      queryParams: { reason: 'share' },
      replaceUrl: true
    });
  }

  private accept(token: string): void {
    this.processing.set(true);
    this.failed.set(false);
    this.status.set('Verifying your account and accepting the secure invitation…');
    this.authFlow.setPostLoginRedirect('/share-invitation');

    this.sharingApi.accept(token).pipe(
      finalize(() => this.processing.set(false))
    ).subscribe({
      next: response => {
        this.invitationStorage.clearPendingToken();
        this.authFlow.clearPostLoginRedirect();
        this.invitationStorage.setSuccess(response.message || 'Document invitation accepted successfully.');
        void this.router.navigateByUrl('/shared-with-me', { replaceUrl: true });
      },
      error: () => {
        this.failed.set(true);
        this.status.set('This invitation could not be accepted. It may be expired, revoked, already used, or belong to another account.');
      }
    });
  }

  private captureTokenFromFragment(): void {
    const rawHash = window.location.hash;
    if (!rawHash || rawHash === '#') return;

    try {
      const params = new URLSearchParams(rawHash.substring(1));
      const token = (params.get('token') ?? '').trim();
      if (token) this.invitationStorage.setPendingToken(token);
    } finally {
      const cleanUrl = window.location.pathname + window.location.search;
      window.history.replaceState(null, document.title, cleanUrl);
    }
  }
}
