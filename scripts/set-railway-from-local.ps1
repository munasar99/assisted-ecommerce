# Reads appsettings.Local.json and pushes secrets to linked Railway service.
# Requires: npm install -g @railway/cli  &&  railway link  (in backend/AssistedEcommerce.Api)

$ErrorActionPreference = "Stop"
$apiDir = Join-Path $PSScriptRoot "..\backend\AssistedEcommerce.Api"
$localPath = Join-Path $apiDir "appsettings.Local.json"

function Invoke-RailwayVar {
    param([string]$Key, [string]$Value)
    $Value | railway variable set $Key --stdin
    if ($LASTEXITCODE -ne 0) {
        throw "Railway failed setting $Key (exit $LASTEXITCODE)"
    }
}

if (-not (Test-Path $localPath)) {
    Write-Error "Ma jiro appsettings.Local.json - samee marka hore."
}

if (-not (Get-Command railway -ErrorAction SilentlyContinue)) {
    Write-Error "Railway CLI ma jiro. Run: npm install -g @railway/cli"
}

Push-Location $apiDir

$cfg = Get-Content $localPath -Raw | ConvertFrom-Json
$mongo = $cfg.MongoDb.ConnectionString

if ([string]::IsNullOrWhiteSpace($mongo) -or $mongo -match "localhost") {
    Pop-Location
    Write-Error "MongoDb.ConnectionString waa localhost ama madhan - geli mongodb+srv Atlas."
}

Write-Host "Setting Railway variables (railway variable set)..." -ForegroundColor Cyan

Invoke-RailwayVar -Key "MONGODB_URI" -Value $mongo
Invoke-RailwayVar -Key "MongoDb__ConnectionString" -Value $mongo
Invoke-RailwayVar -Key "MongoDb__DatabaseName" -Value $cfg.MongoDb.DatabaseName
Invoke-RailwayVar -Key "Cors__Origins__0" -Value "https://assisted-ecommerce.vercel.app"
Invoke-RailwayVar -Key "ASPNETCORE_ENVIRONMENT" -Value "Production"

if ($null -ne $cfg.Jwt -and $cfg.Jwt.Key) {
    Invoke-RailwayVar -Key "Jwt__Key" -Value $cfg.Jwt.Key
} else {
    Invoke-RailwayVar -Key "Jwt__Key" -Value "AssistedEcommerce-Production-Secret-Key-32Chars!!"
}

if ($cfg.Cloudinary.CloudName) {
    Invoke-RailwayVar -Key "Cloudinary__CloudName" -Value $cfg.Cloudinary.CloudName
    Invoke-RailwayVar -Key "Cloudinary__ApiKey" -Value $cfg.Cloudinary.ApiKey
    Invoke-RailwayVar -Key "Cloudinary__ApiSecret" -Value $cfg.Cloudinary.ApiSecret
    Invoke-RailwayVar -Key "Cloudinary__Folder" -Value $cfg.Cloudinary.Folder
}

if ($cfg.Resend.ApiKey) {
    Invoke-RailwayVar -Key "Resend__ApiKey" -Value $cfg.Resend.ApiKey
    Invoke-RailwayVar -Key "Resend__FromEmail" -Value $cfg.Resend.FromEmail
    Invoke-RailwayVar -Key "Resend__Enabled" -Value "true"
    Invoke-RailwayVar -Key "Resend__UseDevelopmentRedirect" -Value "false"
}

Write-Host ""
Write-Host "Variables:" -ForegroundColor Yellow
railway variable list

Write-Host ""
Write-Host "Redeploying..." -ForegroundColor Cyan
railway redeploy -y 2>$null
if ($LASTEXITCODE -ne 0) {
    railway redeploy
}

Pop-Location

Write-Host ""
Write-Host "Done. Sug 2-3 daq kadib:" -ForegroundColor Green
Write-Host "https://assisted-ecommerce-api-production.up.railway.app/api/health/mongodb"
