param(
    [string]$ProjectPath = ".\backend\PersonalDigitalVault.Api",
    [string]$BaseUrl = "https://localhost:7240"
)

$ErrorActionPreference = "Stop"

function Require-Command([string]$Name) {
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' was not found in PATH."
    }
}

Require-Command "dotnet"

$resolvedProject = Resolve-Path $ProjectPath
Push-Location $resolvedProject

try {
    Write-Host "Personal Digital Vault - Stripe local configuration" -ForegroundColor Cyan
    Write-Host "Secrets are stored with .NET User Secrets and are not written to appsettings.json." -ForegroundColor DarkGray
    Write-Host ""

    $apiKey = Read-Host "Stripe sandbox server API key (recommended: rk_test_..., or sk_test_...)"
    if ([string]::IsNullOrWhiteSpace($apiKey) -or
        (-not $apiKey.StartsWith("rk_test_") -and -not $apiKey.StartsWith("sk_test_"))) {
        throw "Expected a Stripe sandbox restricted key (rk_test_...) or secret key (sk_test_...)."
    }

    $productId = Read-Host "Stripe Product ID (prod_...)"
    if ([string]::IsNullOrWhiteSpace($productId) -or -not $productId.StartsWith("prod_")) {
        throw "Expected a Stripe Product ID beginning with prod_."
    }

    $priceId = Read-Host "Stripe recurring Price ID (price_...)"
    if ([string]::IsNullOrWhiteSpace($priceId) -or -not $priceId.StartsWith("price_")) {
        throw "Expected a Stripe recurring Price ID beginning with price_."
    }

    $webhookSecret = Read-Host "Stripe CLI webhook signing secret (whsec_...)"
    if ([string]::IsNullOrWhiteSpace($webhookSecret) -or -not $webhookSecret.StartsWith("whsec_")) {
        throw "Expected a Stripe webhook signing secret beginning with whsec_."
    }

    dotnet user-secrets set "Stripe:SecretKey" $apiKey | Out-Null
    dotnet user-secrets set "Stripe:ProductId" $productId | Out-Null
    dotnet user-secrets set "Stripe:PriceId" $priceId | Out-Null
    dotnet user-secrets set "Stripe:WebhookSecret" $webhookSecret | Out-Null
    dotnet user-secrets set "Stripe:Environment" "Sandbox" | Out-Null
    dotnet user-secrets set "App:BaseUrl" $BaseUrl | Out-Null

    Write-Host ""
    Write-Host "Stripe local secrets saved successfully." -ForegroundColor Green
    Write-Host "Environment: Sandbox"
    Write-Host "App Base URL: $BaseUrl"
    Write-Host ""
    Write-Host "Next:" -ForegroundColor Yellow
    Write-Host "  dotnet restore"
    Write-Host "  dotnet ef database update"
    Write-Host "  dotnet run --launch-profile https"
}
finally {
    Pop-Location
}
