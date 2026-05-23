# Sets ONLY MongoDB on Railway (fixes localhost /health/mongodb).
# Usage: cd backend/AssistedEcommerce.Api && railway link
#        cd ../.. && powershell -File scripts/set-mongo-railway.ps1

$ErrorActionPreference = "Stop"
$localPath = Join-Path $PSScriptRoot "..\backend\AssistedEcommerce.Api\appsettings.Local.json"
$apiDir = Join-Path $PSScriptRoot "..\backend\AssistedEcommerce.Api"

if (-not (Test-Path $localPath)) { Write-Error "Ma jiro appsettings.Local.json" }
if (-not (Get-Command railway -ErrorAction SilentlyContinue)) { Write-Error "Install: npm install -g @railway/cli" }

$mongo = (Get-Content $localPath -Raw | ConvertFrom-Json).MongoDb.ConnectionString
if ($mongo -match "localhost") { Write-Error "Geli mongodb+srv Atlas appsettings.Local.json" }

Push-Location $apiDir
Write-Host "Setting MONGODB_URI on Railway..." -ForegroundColor Cyan
$mongo | railway variable set MONGODB_URI --stdin
$mongo | railway variable set MongoDb__ConnectionString --stdin
railway variable set MongoDb__DatabaseName=ubaxsana
Write-Host ""
railway variable list
Write-Host ""
railway redeploy
Pop-Location
Write-Host "Test: https://assisted-ecommerce-api-production.up.railway.app/api/health/mongodb" -ForegroundColor Green
