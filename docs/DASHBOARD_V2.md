# User Dashboard V2

## Endpoint

`GET /api/dashboard`

Authenticated `User` role only.

## Dashboard response contains only safe metadata

- User display name
- Folder/document/credential/shared-with-me counts
- Subscription quota/usage
- Active premium provider/status/next billing metadata
- Email verification and TOTP status
- Last 5 owned document metadata (name, folder, size, upload time)
- Last 4 folders and document counts

It does **not** return decrypted document bytes, credentials, encryption keys, storage paths, hashes, PayPal secrets, Stripe secrets, or payment-card data.

## UX sections

1. Personalized greeting + vault health
2. Four primary summary cards
3. Storage quota progress
4. Plan/billing card with PayPal/Stripe-aware action
5. Quick actions
6. Recent documents
7. Security status
8. Recent folders

## Database

No migration is required for Dashboard V2.
