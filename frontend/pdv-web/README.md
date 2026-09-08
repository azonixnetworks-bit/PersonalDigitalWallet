# Personal Digital Vault — Angular Migration Phase 4

This is the cumulative Angular frontend after Phase 1 + Phase 2 + Phase 3 + Phase 4. It runs side-by-side with the existing ASP.NET Core 8 Web API. Backend/database/encryption/payment contracts remain unchanged.

## Frozen compatibility decisions

- Angular: 22.1.4 standalone architecture
- API base: `/api`
- Local API target: `https://localhost:7240`
- Final access JWT: `localStorage['pdv_token']` (existing project behavior preserved)
- Registration/TOTP/MFA temporary values: `sessionStorage` only
- Password-reset token: component memory only; never browser storage
- ASP.NET API remains the authority for authentication/authorization
- Existing legacy CSS remains under `src/styles/legacy` until each feature reaches parity

## Phase 4 implemented

- Full Angular Dashboard V2 replacing the placeholder
- Typed `DashboardResponse` model tree
- Real `GET /api/dashboard` integration
- Summary cards, storage quota/progress and warnings
- PayPal/Stripe-aware plan/billing metadata and Angular billing routes
- Quick actions with Angular Router links
- Recent documents and recent folders
- Security status checks
- Loading, empty, error, retry and manual refresh UX
- `/html/dashboard.html` migration alias

## Prerequisites

Use a Node version supported by this Angular 22 package configuration: `^22.22.3`, `^24.15.0`, or `^26.0.0`.

## Run locally

1. Start the original ASP.NET Core API.
2. Open `frontend/pdv-web`.
3. Run `npm install`.
4. Run `npm run build`.
5. Run `npm start`.
6. Open `http://localhost:4200/dashboard`.

The dev server proxies `/api/*` to `https://localhost:7240` through `proxy.conf.json`.

Do not remove the original `wwwroot` frontend yet because remaining feature pages will be migrated in later phases.
