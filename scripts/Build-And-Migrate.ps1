param(
    [string]$ProjectPath = ".\backend\PersonalDigitalVault.Api"
)

$ErrorActionPreference = "Stop"
$resolvedProject = Resolve-Path $ProjectPath
Push-Location $resolvedProject

try {
    dotnet restore
    dotnet build --no-restore
    dotnet ef database update
    Write-Host "Build and database migration completed." -ForegroundColor Green
}
finally {
    Pop-Location
}
