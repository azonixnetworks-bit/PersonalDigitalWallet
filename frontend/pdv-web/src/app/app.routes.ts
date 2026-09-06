import { Routes } from '@angular/router';
import { adminGuard } from './core/guards/admin.guard';
import { authGuard } from './core/guards/auth.guard';
import { guestGuard } from './core/guards/guest.guard';
import { AdminShellComponent } from './shared/components/admin-shell/admin-shell.component';
import { AppShellComponent } from './shared/components/app-shell/app-shell.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'login' },
  {
    path: '',
    canActivateChild: [guestGuard],
    children: [
      { path: 'login', loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent) },
      { path: 'register', loadComponent: () => import('./features/auth/register/register.component').then(m => m.RegisterComponent) },
      { path: 'forgot-password', loadComponent: () => import('./features/auth/forgot-password/forgot-password.component').then(m => m.ForgotPasswordComponent) },
      { path: 'reset-password', loadComponent: () => import('./features/auth/reset-password/reset-password.component').then(m => m.ResetPasswordComponent) }
    ]
  },
  { path: 'verify-email', loadComponent: () => import('./features/auth/verify-email/verify-email.component').then(m => m.VerifyEmailComponent) },
  { path: 'setup-totp', loadComponent: () => import('./features/auth/setup-totp/setup-totp.component').then(m => m.SetupTotpComponent) },
  { path: 'verify-login-otp', loadComponent: () => import('./features/auth/verify-login-otp/verify-login-otp.component').then(m => m.VerifyLoginOtpComponent) },
  { path: 'verify-login-totp', redirectTo: 'verify-login-otp', pathMatch: 'full' },
  {
    path: '',
    component: AppShellComponent,
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent) },
      { path: 'vault/folders', loadComponent: () => import('./features/vault/folders.component').then(m => m.FoldersComponent) },
      { path: 'vault/documents', loadComponent: () => import('./features/vault/documents.component').then(m => m.DocumentsComponent) },
      { path: 'vault/credentials', loadComponent: () => import('./features/vault/credentials.component').then(m => m.CredentialsComponent) },
      { path: 'search', loadComponent: () => import('./features/search/search.component').then(m => m.SearchComponent) },
      { path: 'shared-with-me', loadComponent: () => import('./features/sharing/shared-with-me.component').then(m => m.SharedWithMeComponent) },
      { path: 'profile', loadComponent: () => import('./features/profile/profile.component').then(m => m.ProfileComponent) },
      { path: 'subscription', loadComponent: () => import('./features/subscription/subscription.component').then(m => m.SubscriptionComponent) },
      { path: 'stripe-subscription', loadComponent: () => import('./features/subscription/stripe-subscription.component').then(m => m.StripeSubscriptionComponent) }
    ]
  },
  {
    path: 'admin',
    component: AdminShellComponent,
    canActivate: [authGuard, adminGuard],
    canActivateChild: [adminGuard],
    children: [
      { path: '', pathMatch: 'full', loadComponent: () => import('./features/admin/admin.component').then(m => m.AdminComponent) },
      { path: 'subscriptions', loadComponent: () => import('./features/admin/admin-subscriptions.component').then(m => m.AdminSubscriptionsComponent) }
    ]
  },

  // Legacy URL aliases ease the transition from the old wwwroot HTML frontend.
  { path: 'html/login.html', redirectTo: 'login', pathMatch: 'full' },
  { path: 'html/register.html', redirectTo: 'register', pathMatch: 'full' },
  { path: 'html/verify-email.html', redirectTo: 'verify-email', pathMatch: 'full' },
  { path: 'html/setup-totp.html', redirectTo: 'setup-totp', pathMatch: 'full' },
  { path: 'html/verify-login-otp.html', redirectTo: 'verify-login-otp', pathMatch: 'full' },
  { path: 'html/forgot-password.html', redirectTo: 'forgot-password', pathMatch: 'full' },
  {
    path: 'html/reset-password.html',
    loadComponent: () => import('./features/auth/reset-password/reset-password.component').then(m => m.ResetPasswordComponent)
  },

  { path: '**', redirectTo: 'login' }
];
