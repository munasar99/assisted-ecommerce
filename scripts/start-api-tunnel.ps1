# Card la'aan: API guriga + tunnel Vercel u xiro
# 1) Soo dejiso cloudflared: https://developers.cloudflare.com/cloudflare-one/connections/connect-networks/downloads/
# 2) Run: .\scripts\start-api-tunnel.ps1

$ErrorActionPreference = "Stop"
$apiDir = Join-Path $PSScriptRoot "..\backend\AssistedEcommerce.Api"

Write-Host "Starting API..." -ForegroundColor Cyan
$api = Start-Process -FilePath "dotnet" -ArgumentList "run" -WorkingDirectory $apiDir -PassThru -NoNewWindow

Start-Sleep -Seconds 8

Write-Host "Starting Cloudflare tunnel (copy HTTPS URL)..." -ForegroundColor Cyan
Write-Host "Vercel -> VITE_API_URL = https://YOUR-URL/api" -ForegroundColor Yellow
cloudflared tunnel --url http://localhost:5298

if ($api -and !$api.HasExited) { Stop-Process -Id $api.Id -Force -ErrorAction SilentlyContinue }
