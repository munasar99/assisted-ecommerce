# Reads appsettings.Local.json and pushes secrets to linked Railway service.
# Run from repo root after: cd backend/AssistedEcommerce.Api && railway link

$ErrorActionPreference = "Stop"
$apiDir = Join-Path $PSScriptRoot "..\backend\AssistedEcommerce.Api"
$localPath = Join-Path $apiDir "appsettings.Local.json"

if (-not (Test-Path $localPath)) {
    Write-Error "Ma jiro: $localPath — samee appsettings.Local.json marka hore."
}

if (-not (Get-Command railway -ErrorAction SilentlyContinue)) {
    Write-Error "Railway CLI ma jiro. Run: npm install -g @railway/cli"
}

Push-Location $apiDir
try {
    $cfg = Get-Content $localPath -Raw | ConvertFrom-Json

    $mongo = $cfg.MongoDb.ConnectionString
    if ([string]::IsNullOrWhiteSpace($mongo) -or $mongo -match "localhost") {
        Write-Error "MongoDb.ConnectionString waa localhost ama madhan — geli mongodb+srv:// Atlas."
    }

    Write-Host "Setting Railway variables from appsettings.Local.json ..." -ForegroundColor Cyan

    railway variables set "MONGODB_URI=$mongo"
    railway variables set "MongoDb__DatabaseName=$($cfg.MongoDb.DatabaseName)"
    railway variables set "MongoDb__ConnectionString=$mongo"

    if ($cfg.Jwt.Key) { railway variables set "Jwt__Key=$($cfg.Jwt.Key)" }

    railway variables set "Cors__Origins__0=https://assisted-ecommerce.vercel.app"
    railway variables set "ASPNETCORE_ENVIRONMENT=Production"

    if ($cfg.Cloudinary.CloudName) {
        railway variables set "Cloudinary__CloudName=$($cfg.Cloudinary.CloudName)"
        railway variables set "Cloudinary__ApiKey=$($cfg.Cloudinary.ApiKey)"
        railway variables set "Cloudinary__ApiSecret=$($cfg.Cloudinary.ApiSecret)"
        railway variables set "Cloudinary__Folder=$($cfg.Cloudinary.Folder)"
    }

    if ($cfg.Resend.ApiKey) {
        railway variables set "Resend__ApiKey=$($cfg.Resend.ApiKey)"
        railway variables set "Resend__FromEmail=$($cfg.Resend.FromEmail)"
        railway variables set "Resend__Enabled=true"
        railway variables set "Resend__UseDevelopmentRedirect=false"
    }

    Write-Host ""
    Write-Host "Variables list:" -ForegroundColor Yellow
    railway variables

    Write-Host ""
    Write-Host "Redeploying..." -ForegroundColor Cyan
    railway redeploy

    Write-Host ""
    Write-Host "Done. Sug 2-3 daq kadib tijaabi:" -ForegroundColor Green
    Write-Host "https://assisted-ecommerce-api-production.up.railway.app/api/health/mongodb"
}
finally {
    Pop-Location
}
