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

$values = [ordered]@{
    POSTGRES_PASSWORD        = New-Secret
    JWT_KEY                  = New-Secret
    SERVICE_API_KEY          = New-Secret
    FRONTEND_ORIGIN          = 'http://localhost:5173'
    WASMGATE_ADMIN_TOKEN     = New-Secret
    WASMGATE_CP_ADMIN_TOKEN  = New-Secret
}

$envPath = Join-Path $root '.env'
if (-not (Test-Path $envPath)) {
    $existingVolume = docker volume ls --quiet --filter name=microservices_auth_pgdata
    if ($existingVolume -eq 'microservices_auth_pgdata') {
        throw 'Existing Postgres volume found without .env. Restore .env or run "docker compose down -v" first.'
    }

    $content = ($values.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" }) -join [Environment]::NewLine
    [System.IO.File]::WriteAllText($envPath, $content + [Environment]::NewLine, (New-Object System.Text.UTF8Encoding($false)))
    Write-Host 'Created .env with generated secrets.'
}
else {
    $content = Get-Content $envPath -Raw
    $append = @()
    foreach ($key in $values.Keys) {
        if ($content -notmatch "(?m)^$key=") {
            $append += "$key=$($values[$key])"
        }
    }

    if ($append.Count -gt 0) {
        [System.IO.File]::AppendAllText($envPath, (($append -join [Environment]::NewLine) + [Environment]::NewLine), (New-Object System.Text.UTF8Encoding($false)))
        Write-Host 'Added missing values to .env.'
    }
    else {
        Write-Host 'Using existing .env.'
    }
}

docker compose up -d --build
if ($LASTEXITCODE -ne 0) {
    throw "docker compose failed with exit code $LASTEXITCODE"
}

function Read-EnvValue([string]$key) {
    return (Get-Content $envPath | Where-Object { $_ -like "$key=*" } | Select-Object -First 1) -replace "^$key=", ''
}

$adminToken = Read-EnvValue 'WASMGATE_ADMIN_TOKEN'
$cpToken = Read-EnvValue 'WASMGATE_CP_ADMIN_TOKEN'

Write-Host ''
Write-Host 'Microservices stack is up:'
Write-Host '  Public API / SignalR : http://localhost:8080'
Write-Host '  Swagger              : http://localhost:7001/swagger, :7002/swagger, :7003/swagger'
Write-Host '  WasmGate admin       : http://localhost:19099 (Bearer token from .env)'
Write-Host '  WasmGate CP UI       : http://localhost:19097 (Bearer token from .env)'
Write-Host '  PostgreSQL via edge  : localhost:5433 (AuthDb/TaskDb/NotificationsDb)'
if ($adminToken) {
    Write-Host "  WasmGate token       : $adminToken"
}
if ($cpToken) {
    Write-Host "  Control plane token  : $cpToken"
}
