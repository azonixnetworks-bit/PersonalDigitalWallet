import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { finalize, take } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';
import { AppBrandComponent } from '../app-brand/app-brand.component';
import { ThemeService } from '../../services/theme.service';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, AppBrandComponent],
  templateUrl: './app-topbar.component.html',
  styleUrl: './app-topbar.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AppTopbarComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly themeService = inject(ThemeService);
  readonly menuOpen = signal(false);
  readonly loggingOut = signal(false);

  toggleMenu(): void { this.menuOpen.update(value => !value); }
  closeMenu(): void { this.menuOpen.set(false); }
  toggleTheme(): void { this.themeService.toggle(); }

  logout(): void {
    if (this.loggingOut()) return;
    this.loggingOut.set(true);
    this.auth.logout().pipe(
      take(1),
      finalize(() => this.finishLogout())
    ).subscribe({ error: () => undefined });
  }

  private finishLogout(): void {
    this.auth.clearLocalSession();
    this.loggingOut.set(false);
    void this.router.navigateByUrl('/login', { replaceUrl: true });
  }
}
