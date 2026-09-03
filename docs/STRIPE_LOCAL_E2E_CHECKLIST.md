# Stripe + PayPal Parallel Local E2E Checklist

## Pre-flight

- [ ] Existing PayPal source files still exist.
- [ ] Existing PayPal database columns still exist.
- [ ] `20260902000100_ReplacePayPalWithStripe.cs` is the SAFE NO-OP version.
- [ ] `20260902000200_AddStripeSubscriptionSupport.cs` exists.
- [ ] `dotnet restore` PASS.
- [ ] `dotnet build` PASS.
- [ ] `dotnet ef database update` PASS.

## PayPal regression

- [ ] `/html/subscription.html` loads.
- [ ] `/api/subscriptions/config` still uses PayPal.
- [ ] Existing PayPal subscription lookup works.
- [ ] PayPal webhook code remains registered.
- [ ] Valid ACTIVE PayPal subscription grants Premium.

## Stripe configuration

- [ ] Sandbox Product `prod_...`.
- [ ] Active recurring Price `price_...`.
- [ ] Customer Portal configured.
- [ ] Stripe CLI authenticated.
- [ ] Stripe CLI listener running.
- [ ] `Stripe:SecretKey` configured.
- [ ] `Stripe:ProductId` configured.
- [ ] `Stripe:PriceId` configured.
- [ ] `Stripe:WebhookSecret` configured.
- [ ] `Stripe:Environment=Sandbox`.

## Stripe happy path

- [ ] `/html/stripe-subscription.html` loads.
- [ ] Price/currency/interval match Stripe.
- [ ] Checkout opens.
- [ ] Sandbox Checkout succeeds.
- [ ] `checkout.session.completed` received.
- [ ] subscription lifecycle event received.
- [ ] invoice event received.
- [ ] local row has `Provider=Stripe`.
- [ ] Stripe customer/subscription/price IDs are correct.
- [ ] status ACTIVE/TRIALING.
- [ ] Premium limits = 200 docs / 500 MB.
- [ ] Customer Portal opens for correct customer.

## Dual-provider behavior

- [ ] Stripe row does not overwrite a PayPal row.
- [ ] PayPal row does not overwrite a Stripe row.
- [ ] Multiple Stripe users can exist without PayPal unique-index conflicts.
- [ ] Inactive Stripe does not disable a valid active PayPal entitlement.
- [ ] Inactive PayPal does not disable a valid active Stripe entitlement.
- [ ] Active Premium on either provider prevents duplicate Stripe checkout.

## Security

- [ ] Invalid Stripe webhook signature rejected.
- [ ] Wrong Sandbox/Live mode rejected.
- [ ] Wrong Stripe Price ID does not grant Premium.
- [ ] Another user's Checkout Session does not grant Premium.
- [ ] Stripe server key absent from browser code.
- [ ] Stripe webhook secret absent from browser code.
- [ ] No card data stored in PDV.
- [ ] PayPal secrets remain server-side.
