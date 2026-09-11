param(
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",

    [string]$PublishPath = ".\artifacts\pdv-production",

    [switch]$SkipNpmInstall
)

$ErrorActionPreference = "Stop"

function Assert-LastExitCode {
    param(
        [string]$Step
    )

    if ($LASTEXITCODE -ne 0) {
        throw "$Step failed with exit code $LASTEXITCODE."
    }
}

$repoRoot =
    (Resolve-Path(
        (Join-Path $PSScriptRoot "..")
    )).Path

$frontendPath =
    Join-Path $repoRoot "frontend\pdv-web"

$backendProject =
    Join-Path $repoRoot "backend\PersonalDigitalVault.Api\PersonalDigitalVault.Api.csproj"

if ([System.IO.Path]::IsPathRooted($PublishPath)) {
    $publishFullPath = $PublishPath
}
else {
    $publishFullPath =
        Join-Path $repoRoot $PublishPath
}

Write-Host ""
Write-Host "PDV Angular production cutover publish" -ForegroundColor Cyan
Write-Host "Repository : $repoRoot"
Write-Host "Frontend   : $frontendPath"
Write-Host "Backend    : $backendProject"
Write-Host "Publish    : $publishFullPath"
Write-Host ""

if (-not (Test-Path $frontendPath)) {
    throw "Angular frontend folder was not found: $frontendPath"
}

if (-not (Test-Path $backendProject)) {
    throw "Backend project was not found: $backendProject"
}

Push-Location $frontendPath

try {
   if ((-not $SkipNpmInstall) -and (-not (Test-Path (Join-Path $frontendPath "node_modules")))) {
        Write-Host "Installing Angular dependencies without creating package-lock.json..." -ForegroundColor Yellow
        npm install --no-package-lock
        Assert-LastExitCode "npm install"
    }

    Write-Host "Building Angular production bundle..." -ForegroundColor Yellow
    npm run build:production
    Assert-LastExitCode "Angular production build"
}
finally {
    Pop-Location
}

$angularCandidates = @(
    (Join-Path $frontendPath "dist\pdv-web\browser"),
    (Join-Path $frontendPath "dist\pdv-web")
)

$angularOutput =
    $angularCandidates |
    Where-Object {
        Test-Path (Join-Path $_ "index.html")
    } |
    Select-Object -First 1

if ([string]::IsNullOrWhiteSpace($angularOutput)) {
    throw "Angular output index.html was not found under dist\pdv-web."
}

if (Test-Path $publishFullPath) {
    Remove-Item $publishFullPath -Recurse -Force
}

New-Item -ItemType Directory -Path $publishFullPath -Force | Out-Null

Write-Host "Publishing .NET backend..." -ForegroundColor Yellow
dotnet publish $backendProject -c $Configuration -o $publishFullPath
Assert-LastExitCode ".NET publish"

$publishedWwwroot =
    Join-Path $publishFullPath "wwwroot"

if (Test-Path $publishedWwwroot) {
    Remove-Item $publishedWwwroot -Recurse -Force
}

New-Item -ItemType Directory -Path $publishedWwwroot -Force | Out-Null

Write-Host "Copying Angular bundle into published wwwroot..." -ForegroundColor Yellow

Get-ChildItem -Path $angularOutput -Force |
    Copy-Item -Destination $publishedWwwroot -Recurse -Force

$publishedIndex =
    Join-Path $publishedWwwroot "index.html"

if (-not (Test-Path $publishedIndex)) {
    throw "Published Angular index.html is missing."
}

$legacyHtmlDirectory =
    Join-Path $publishedWwwroot "html"

if (Test-Path $legacyHtmlDirectory) {
    throw "Legacy html directory unexpectedly exists in published wwwroot."
}

Write-Host ""
Write-Host "Production publish PASS." -ForegroundColor Green
Write-Host "Angular source : $angularOutput" -ForegroundColor Green
Write-Host "Output         : $publishFullPath" -ForegroundColor Green
Write-Host ""
Write-Host "The publish output contains the Angular frontend in wwwroot and the .NET API in one deployable folder." -ForegroundColor Green
