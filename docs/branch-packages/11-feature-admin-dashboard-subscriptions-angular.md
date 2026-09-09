# Phase 11 - Admin Dashboard + Billing Administration Angular Migration

Branch: `feature/admin-dashboard-subscriptions-angular`
Base: `development` at `bb968424db9227899fb5166747ba31b0151f89b6`
Frontend package version: `1.0.8-phase11`

## Scope

- Replace the Angular Admin Dashboard placeholder with a real premium-white administrator console.
- Load the existing safe admin dashboard metadata from `GET /api/admin/dashboard`.
- Load the existing user list from `GET /api/admin/users`.
- Support client-side user search.
- Allow Admin to enable or disable normal user accounts through `PUT /api/admin/users/{id}/status`.
- Keep Admin accounts protected from account-status actions in the Angular UI; backend protection remains authoritative.
- Show recent upload metadata only: uploaded-by, original file name, file size and upload timestamp.
- Replace the Angular Admin Subscriptions placeholder with a read-only billing metadata page.
- Load `GET /api/admin/subscriptions/summary` and `GET /api/admin/subscriptions`.
- Show summary counts, provider, plan, lifecycle status, masked provider reference, next billing date and last verified date.
- Add loading, empty, retry and error states.
- Add legacy aliases `/html/admin.html` -> `/admin` and `/html/admin-subscriptions.html` -> `/admin/subscriptions`.

## Security freeze

- No backend, database, migration, authentication, authorization, entitlement, Stripe, PayPal or webhook changes.
- Existing Angular `authGuard` and `adminGuard` remain in place.
- Backend `[Authorize(Roles = "Admin")]` remains the authorization authority.
- Administrator UI never requests document contents, downloads, credentials, search results, shares, encryption material or storage paths.
- Upload activity remains metadata-only.
- Subscription administration remains read-only.
- Browser does not activate, cancel, suspend, resume or otherwise modify billing subscriptions.
- Provider subscription references displayed by Angular are the backend-masked values only.
- No provider secrets are added to the frontend.

## Existing API contracts used

- `GET /api/admin/dashboard`
- `GET /api/admin/users`
- `PUT /api/admin/users/{id}/status`
- `GET /api/admin/subscriptions/summary`
- `GET /api/admin/subscriptions`

## Expected validation

- Angular `ng build` PASS.
- `git diff --check` PASS.
- No generated `package-lock.json` tracked.
- Exact Phase 11 delta audited before commit and before PR merge.
- Feature PR targets `development`, never `main`.
- Use a normal merge commit after explicit merge approval.
