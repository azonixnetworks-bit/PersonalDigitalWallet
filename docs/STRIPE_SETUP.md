# Stripe Parallel Subscription Setup — Personal Digital Vault

PayPal remains enabled. Stripe is added as a second subscription provider.

## Final provider architecture

```text
PayPal UI/API
  /html/subscription.html
  /api/subscriptions/*
          |
          v
     Subscriptions table
          ^
          |
Stripe UI/API
  /html/stripe-subscription.html
  /api/stripe/subscriptions/*

Stripe webhook:
  POST /api/stripe/webhook
```

Both providers share the same backend Premium entitlement service.

## Database safety

The corrected migration sequence is:

```text
20260902000100_ReplacePayPalWithStripe
    SAFE NO-OP compatibility placeholder

20260902000200_AddStripeSubscriptionSupport
    adds Stripe fields without deleting PayPal data
```

`AddStripeSubscriptionSupport`:

- keeps `PayPalSubscriptionId`;
- keeps `PayPalPlanId`;
- makes PayPal provider IDs nullable so Stripe rows can coexist;
- keeps the PayPal unique index as a filtered unique index;
- adds `Users.StripeCustomerId`;
- adds `Subscriptions.StripeSubscriptionId`;
- adds `Subscriptions.StripePriceId`;
- adds `Subscriptions.CancelAtPeriodEnd`;
- adds filtered unique Stripe indexes.

Important: if the OLD destructive `20260902000100_ReplacePayPalWithStripe`
migration was already successfully applied to your database before installing
this corrected package, restore the database backup first. In the current
reported workflow the project failed at `dotnet build`, so that migration should
not have been applied.

## 1. Stripe Sandbox Product + recurring Price

Create a Stripe sandbox/test Product for Premium and an active recurring Price.

Copy:

```text
prod_...
price_...
```

## 2. Stripe server key

Use a sandbox server API key:

```text
rk_test_...
```

or:

```text
sk_test_...
```

Do not put it in source code.

## 3. Customer Portal

Enable Stripe Customer Portal in Sandbox/Test mode so the customer can:

- update payment method;
- view billing details;
- cancel subscription.

## 4. Stripe CLI

Install Stripe CLI and authenticate:

```powershell
stripe login
```

## 5. Start local webhook listener

From project root:

```powershell
.\scripts\Listen-Stripe-Local.ps1
```

It forwards to:

```text
https://localhost:7240/api/stripe/webhook
```

Copy the CLI signing secret:

```text
whsec_...
```

Keep that terminal open.

## 6. Configure Stripe User Secrets

In a second PowerShell terminal from project root:

```powershell
.\scripts\Configure-Stripe-Local.ps1
```

The script stores:

```text
Stripe:SecretKey
Stripe:ProductId
Stripe:PriceId
Stripe:WebhookSecret
Stripe:Environment = Sandbox
App:BaseUrl = https://localhost:7240
```

The hosted Checkout flow does not need a Stripe publishable key in the browser.

Existing PayPal User Secrets are not changed or deleted.

## 7. Build + migration

```powershell
.\scripts\Build-And-Migrate.ps1
```

Equivalent:

```powershell
cd .\backend\PersonalDigitalVault.Api
dotnet restore
dotnet build
dotnet ef database update
```

Expected:

```text
Build succeeded.
0 Error(s)
```

## 8. Run

```powershell
cd .\backend\PersonalDigitalVault.Api
dotnet run --launch-profile https
```

PayPal page:

```text
https://localhost:7240/html/subscription.html
```

Stripe page:

```text
https://localhost:7240/html/stripe-subscription.html
```

## 9. Provider APIs

Existing PayPal APIs remain unchanged:

```text
GET  /api/subscriptions/config
GET  /api/subscriptions/me
POST /api/subscriptions/confirm
POST /api/subscriptions/cancel
```

Stripe APIs are separate:

```text
GET  /api/stripe/subscriptions/config
GET  /api/stripe/subscriptions/me
POST /api/stripe/subscriptions/checkout-session
POST /api/stripe/subscriptions/verify-checkout
POST /api/stripe/subscriptions/portal-session
```

Stripe webhook:

```text
POST /api/stripe/webhook
```

## 10. Premium entitlement

FREE:

- 20 documents
- 50 MB

PREMIUM:

- 200 documents
- 500 MB

PayPal grants Premium only when:

```text
Provider = PayPal
Status = ACTIVE
PayPalPlanId = configured PayPal:PlanId
```

Stripe grants Premium only when:

```text
Provider = Stripe
Status = ACTIVE or TRIALING
StripePriceId = configured Stripe:PriceId
```

An inactive subscription on one provider does not hide an active valid
subscription on the other provider.

## 11. Stripe E2E

1. Keep Stripe CLI listener running.
2. Run PDV API.
3. Login with an active, email-verified, TOTP-enabled User.
4. Open `/html/stripe-subscription.html`.
5. Confirm amount/currency/interval match Stripe Dashboard.
6. Click **Upgrade with Stripe**.
7. Complete sandbox Checkout.
8. Return to PDV.
9. Confirm Checkout verification succeeds.
10. Confirm webhook events arrive.
11. Confirm local Stripe subscription is ACTIVE/TRIALING.
12. Confirm Premium entitlement is enabled.
13. Open **Manage Billing** and verify Customer Portal.
14. Test cancel-at-period-end and payment failure behavior.
15. Re-check existing PayPal subscription page/regression.

## 12. Production

Use separate Live:

```text
Stripe:Environment = Live
prod_...
price_...
Live server API key
Live webhook whsec_...
```

Register the public production webhook:

```text
https://YOUR_DOMAIN/api/stripe/webhook
```

Never mix Sandbox keys/objects with Live configuration.
