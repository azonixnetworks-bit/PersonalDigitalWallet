# Phase 10 — User Subscription + Stripe Billing Angular Migration

Branch: `feature/subscription-stripe-angular`
Base: `development` at `af63690e757acd20767217b774308d0d24880496`
Frontend package version: `1.0.7-phase10`

## Scope

- Replace the Angular Subscription placeholder with a real premium-white billing page.
- Load Stripe configuration, current Stripe subscription, and current backend entitlement data.
- Show current plan, provider/status lifecycle, document usage, storage usage, remaining allowance, and maximum upload size.
- Start Stripe-hosted Checkout through the existing backend endpoint.
- Verify `checkout=success&session_id=...` through the existing backend before showing verified Premium state.
- Treat checkout cancellation as a non-activation state.
- Open Stripe Customer Portal through the existing backend endpoint.
- Keep `/stripe-subscription` as a compatibility wrapper around the canonical `/subscription` implementation.
- Redirect legacy `/html/subscription.html` and `/html/stripe-subscription.html` URLs to `/subscription`.

## Security freeze

- No backend, database, migration, authentication, role, entitlement-policy, webhook, or Stripe-secret changes.
- Browser does not collect card details.
- Browser does not grant Premium access from a redirect alone.
- Premium state displayed by Angular comes only from PDV backend responses.
- Checkout and portal redirects are accepted only for HTTPS Stripe domains.
- PayPal legacy UI is not migrated in this phase.
- Admin Angular placeholders are not changed in this phase.

## Existing API contracts used

- `GET /api/stripe/subscriptions/config`
- `GET /api/stripe/subscriptions/me`
- `POST /api/stripe/subscriptions/checkout-session`
- `POST /api/stripe/subscriptions/verify-checkout`
- `POST /api/stripe/subscriptions/portal-session`
- `GET /api/subscriptions/entitlements`
