import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { finalize, take } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';
import { AppBrandComponent } from '../app-brand/app-brand.component';

@Component({
  selector: 'app-admin-topbar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, AppBrandComponent],
  templateUrl: './admin-topbar.component.html',
  styleUrl: './admin-topbar.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminTopbarComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly menuOpen = signal(false);
  readonly loggingOut = signal(false);
  toggleMenu(): void { this.menuOpen.update(v => !v); }
  closeMenu(): void { this.menuOpen.set(false); }
  logout(): void {
    if (this.loggingOut()) return;
    this.loggingOut.set(true);
    this.auth.logout().pipe(take(1), finalize(() => this.finishLogout())).subscribe({ error: () => undefined });
  }
  private finishLogout(): void {
    this.auth.clearLocalSession();
    this.loggingOut.set(false);
    void this.router.navigateByUrl('/login', { replaceUrl: true });
  }
}
