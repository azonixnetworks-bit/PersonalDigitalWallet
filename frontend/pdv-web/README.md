# Personal Digital Vault — Angular Migration Phase 3

This is the cumulative Angular frontend after Phase 1 + Phase 2 + Phase 3. It runs side-by-side with the existing ASP.NET Core 8 Web API. Backend/database/encryption/payment contracts are unchanged.

## Frozen compatibility decisions

- Angular: 22.1.4 standalone architecture
- API base: `/api`
- Local API target: `https://localhost:7240`
- Final access JWT: `localStorage['pdv_token']` (existing project behavior preserved)
- Registration/TOTP/MFA temporary values: `sessionStorage` only
- Password-reset token: component memory only; never browser storage
- ASP.NET API remains the final authority for authentication/authorization
- Existing legacy CSS stays in `src/styles/legacy` until feature parity is complete

## Phase 3 implemented

- Full registration validation UX
- Email OTP verify + resend
- Authenticator/TOTP QR setup + manual key
- TOTP setup verification and temporary-secret cleanup
- Login password -> MFA challenge -> TOTP -> final JWT
- Safe post-login redirect consumption
- Forgot-password generic-response flow
- Reset-password fragment-token flow with immediate URL cleanup
- Central `AuthFlowStorageService` for temporary auth state
- Legacy HTML auth route aliases for gradual migration
- Shared validation/loading/error UX additions

## Prerequisites

Use a Node version supported by this Angular 22 package configuration: `^22.22.3`, `^24.15.0`, or `^26.0.0`.

## Run locally

1. Start the original ASP.NET Core API.
2. Open `frontend/pdv-web`.
3. Run `npm install`.
4. Run `npm start`.
5. Open `http://localhost:4200`.

The dev server proxies `/api/*` to `https://localhost:7240` through `proxy.conf.json`.

## Phase 3 auth routes

- `/login`
- `/register`
- `/verify-email`
- `/setup-totp`
- `/verify-login-otp`
- `/verify-login-totp` (alias)
- `/forgot-password`
- `/reset-password#token=...`

Legacy auth URL aliases are included to ease the future switch away from ASP.NET `wwwroot/html/*.html`.

## Important reset-email development note

The current backend still creates reset emails pointing to its old `https://localhost:7240/html/reset-password.html#token=...` page. While `wwwroot` remains active, that email will still open the legacy reset screen. The Angular alias is already prepared for the final Angular static-host/base-URL integration phase.

Do not remove the original `wwwroot` frontend yet.

## Next phase

Phase 4 migrates the Dashboard with real API integration and production loading/empty/error/responsive states.
