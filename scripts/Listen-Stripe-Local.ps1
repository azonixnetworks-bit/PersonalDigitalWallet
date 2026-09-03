param(
    [string]$ForwardUrl = "https://localhost:7240/api/stripe/webhook"
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command stripe -ErrorAction SilentlyContinue)) {
    throw "Stripe CLI was not found in PATH. Install it and run 'stripe login' first."
}

Write-Host "Forwarding Stripe sandbox webhook events to:" -ForegroundColor Cyan
Write-Host "  $ForwardUrl"
Write-Host ""
Write-Host "Important: the whsec_... printed by this listener must match Stripe:WebhookSecret in .NET User Secrets." -ForegroundColor Yellow
Write-Host ""

stripe listen `
    --events checkout.session.completed,checkout.session.async_payment_succeeded,customer.subscription.created,customer.subscription.updated,customer.subscription.deleted,customer.subscription.paused,customer.subscription.resumed,invoice.paid,invoice.payment_succeeded,invoice.payment_failed,invoice.payment_action_required `
    --forward-to $ForwardUrl `
    --skip-verify
