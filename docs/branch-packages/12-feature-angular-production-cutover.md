# Phase 12 - Angular Production Cutover + Legacy Backend Frontend Retirement

## Branch

`feature/angular-production-cutover`

## Base

`0ea5803722dc29a83c5327155e9478488085926a`

## Angular package version

`1.0.9-phase12`

## Goal

Make the Angular application the production frontend for the existing ASP.NET Core API while retiring the old backend `wwwroot` HTML/CSS/JavaScript frontend.

## Frozen contracts

Phase 12 must not change:

- database schema or EF migrations
- API request/response contracts
- JWT/TOTP/authentication rules
- role authorization rules
- document or credential encryption
- secure storage behavior
- Stripe/PayPal provider logic or secrets
- subscription entitlement rules
- Admin privacy boundary

## Production hosting design

1. Angular continues to use production API base `/api`.
2. `scripts/Publish-Angular-Production.ps1` builds Angular in production mode.
3. The script publishes the .NET API to `artifacts/pdv-production` by default.
4. The published `wwwroot` is cleared and replaced with the Angular production bundle.
5. `SpaFallbackController` serves Angular `index.html` only for non-API browser routes when a static file was not found.
6. Missing static assets remain `404`.
7. `/api` and `/api/*` are explicitly excluded from the SPA fallback.
8. Existing Angular `/html/*.html` compatibility routes remain available after the legacy frontend files are removed.

## Legacy source retirement

After the Phase 12 overlay is applied and audited, delete these tracked legacy frontend sources from the feature branch:

- `backend/PersonalDigitalVault.Api/wwwroot/assets`
- `backend/PersonalDigitalVault.Api/wwwroot/css`
- `backend/PersonalDigitalVault.Api/wwwroot/html`
- `backend/PersonalDigitalVault.Api/wwwroot/js`
- `backend/PersonalDigitalVault.Api/wwwroot/index.html`
- `backend/PersonalDigitalVault.Api/wwwroot/wwwroot.zip`
- `backend/PersonalDigitalVault.Api/wwwroot/wwwroot_JS_Login_Checked.zip`
- `backend/PersonalDigitalVault.Api/wwwroot.zip`

Keep:

- `backend/PersonalDigitalVault.Api/wwwroot/.gitkeep`

Do **not** delete `frontend/pdv-web/src/styles/legacy` in Phase 12. Angular `src/styles.css` still imports those styles, so they remain a live Angular dependency.

## Validation

Before commit:

1. Confirm branch/base and clean starting point.
2. Apply Phase 12 overlay.
3. Audit exact changed files.
4. Retire the tracked backend legacy frontend files.
5. `git diff --check`
6. Angular production build PASS.
7. .NET backend build PASS.
8. Production publish script PASS.
9. Verify published `wwwroot/index.html` exists.
10. Verify published `wwwroot/html` does not exist.
11. Verify `/api/*` routes remain API routes.
12. Verify Angular deep links such as `/dashboard`, `/vault/documents`, `/subscription`, `/admin`, and legacy `/html/login.html` resolve through the SPA shell.
13. Confirm no `package-lock.json`, secrets, generated `dist`, or `artifacts` files are staged.

## Merge policy

PR target: `development`

Merge method: normal merge commit only.

Do not merge to `main`.

Do not merge until explicit approval is given after the PR changed-files/security audit.
