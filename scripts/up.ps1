$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

function New-Secret {
    $bytes = New-Object byte[] 32
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $rng.GetBytes($bytes)
    }
    finally {
        $rng.Dispose()
    }

    return -join ($bytes | ForEach-Object { $_.ToString('x2') })
}

$envPath = Join-Path $root '.env'
if (-not (Test-Path $envPath)) {
    $existingVolume = docker volume inspect microservices_pgdata 2>$null
    if ($LASTEXITCODE -eq 0) {
        throw 'Existing Postgres volume found without .env. Restore .env or run "docker compose down -v" first.'
    }

    $content = @"
POSTGRES_PASSWORD=$(New-Secret)
JWT_KEY=$(New-Secret)
SERVICE_API_KEY=$(New-Secret)
FRONTEND_ORIGIN=http://localhost:5173
WASMGATE_ADMIN_TOKEN=$(New-Secret)
"@

    [System.IO.File]::WriteAllText($envPath, $content, (New-Object System.Text.UTF8Encoding($false)))
    Write-Host 'Created .env with generated secrets.'
}
else {
    Write-Host 'Using existing .env.'
}

docker compose up -d --build
if ($LASTEXITCODE -ne 0) {
    throw "docker compose failed with exit code $LASTEXITCODE"
}

$adminToken = (Get-Content $envPath | Where-Object { $_ -like 'WASMGATE_ADMIN_TOKEN=*' } | Select-Object -First 1) -replace '^WASMGATE_ADMIN_TOKEN=', ''

Write-Host ''
Write-Host 'Microservices stack is up:'
Write-Host '  Public API / SignalR : http://localhost:8080'
Write-Host '  Swagger              : http://localhost:7001/swagger, :7002/swagger, :7003/swagger'
Write-Host '  WasmGate admin       : http://localhost:9099 (Bearer token from .env)'
if ($adminToken) {
    Write-Host "  WasmGate token       : $adminToken"
}
