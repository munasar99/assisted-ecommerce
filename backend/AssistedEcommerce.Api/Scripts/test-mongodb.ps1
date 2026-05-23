# MongoDB connection test for database "ubaxsana"
$connectionString = "mongodb://localhost:27017"
$databaseName = "ubaxsana"

Write-Host "Testing MongoDB..." -ForegroundColor Cyan
Write-Host "  Connection: $connectionString"
Write-Host "  Database:   $databaseName"
Write-Host ""

try {
    $projectRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
    $apiPath = Join-Path $projectRoot "AssistedEcommerce.Api"
    if (-not (Test-Path $apiPath)) {
        $apiPath = Join-Path (Split-Path $PSScriptRoot -Parent) "."
    }

    Push-Location $apiPath
    dotnet run --no-build 2>&1 | Out-Null
    Pop-Location
} catch {}

# Prefer mongosh if installed
$mongosh = Get-Command mongosh -ErrorAction SilentlyContinue
if ($mongosh) {
    Write-Host "Using mongosh..." -ForegroundColor Green
    mongosh "$connectionString/$databaseName" --eval "db.runCommand({ ping: 1 }); print('Database:', db.getName()); print('Collections:', db.getCollectionNames().join(', ') || '(none yet)');"
    exit $LASTEXITCODE
}

Write-Host "mongosh not found. Start API instead:" -ForegroundColor Yellow
Write-Host "  cd backend/AssistedEcommerce.Api"
Write-Host "  dotnet run"
Write-Host "  Then open: http://localhost:5298/api/health"
Write-Host "  Seed runs on startup -> collections appear in database 'ubaxsana'"
