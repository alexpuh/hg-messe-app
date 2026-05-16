#Requires -Version 5.1
<#
.SYNOPSIS
    Runs Playwright E2E tests for Messe App.

.PARAMETER Headed
    Runs the browser in headed (visible) mode for debugging.

.PARAMETER Inspect
    Opens Playwright Inspector for step-by-step debugging.

.PARAMETER Install
    Installs dependencies (npm ci) and the Chromium browser before running.
    Use on first run or after updating dependencies.

.EXAMPLE
    .\run-e2e.ps1
    .\run-e2e.ps1 -Install
    .\run-e2e.ps1 -Headed
    .\run-e2e.ps1 -Inspect
#>
param(
    [switch]$Headed,
    [switch]$Inspect,
    [switch]$Install
)

$ErrorActionPreference = 'Stop'

$repoRoot = $PSScriptRoot
$e2eDir   = Join-Path $repoRoot 'e2e'

#  Prerequisites check 

Write-Host ""
Write-Host "Checking prerequisites..." -ForegroundColor Cyan

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error ".NET SDK not found. Install .NET 9 SDK from https://dotnet.microsoft.com/download"
}

if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
    Write-Error "Node.js not found. Install Node.js 20+ from https://nodejs.org"
}

$fixtureFile = Join-Path $e2eDir 'fixtures\articles.json'
if (-not (Test-Path $fixtureFile)) {
    Write-Error "Missing fixture file: $fixtureFile`nSee e2e/README.md for details."
}

# Kill any leftover server process from a previous interrupted run so the DB is not locked
$staleServer = Get-Process -Name 'messe-server' -ErrorAction SilentlyContinue
if ($staleServer) {
    Write-Host "Stopping stale messe-server process (PID $($staleServer.Id))..." -ForegroundColor Yellow
    Stop-Process -Id $staleServer.Id -Force
    Start-Sleep -Milliseconds 500
}

# Delete the test DB before Playwright starts the server.
# globalSetup runs AFTER webServer is up, so the DB is already open by then — too late to delete.
$tmpDir  = Join-Path $e2eDir 'tmp'
$dbPath  = Join-Path $tmpDir 'messeapp-e2e.db'
if (-not (Test-Path $tmpDir)) { New-Item -ItemType Directory -Path $tmpDir | Out-Null }
foreach ($file in @($dbPath, "$dbPath-wal", "$dbPath-shm")) {
    if (Test-Path $file) {
        Remove-Item $file -Force
        Write-Host "Deleted $file" -ForegroundColor DarkGray
    }
}

#  Install dependencies 

if ($Install) {
    Write-Host ""
    Write-Host "Installing client dependencies..." -ForegroundColor Cyan
    Push-Location (Join-Path $repoRoot 'client')
    try { npm ci } finally { Pop-Location }

    Write-Host ""
    Write-Host "Installing e2e dependencies..." -ForegroundColor Cyan
    Push-Location $e2eDir
    try {
        npm ci
        npx playwright install --with-deps chromium
    } finally { Pop-Location }
}
elseif (-not (Test-Path (Join-Path $e2eDir 'node_modules'))) {
    # Auto-install when node_modules is missing
    Write-Host ""
    Write-Host "node_modules not found. Running with -Install automatically..." -ForegroundColor Yellow
    & $PSCommandPath -Install:$true -Headed:$Headed -Inspect:$Inspect
    exit $LASTEXITCODE
}
else {
    # Auto-install Chromium if the browser executable is missing (e.g. after a Playwright version bump)
    Push-Location $e2eDir
    try {
        $playwrightDir = Join-Path $env:LOCALAPPDATA 'ms-playwright'
        $chromiumExists = Test-Path $playwrightDir -PathType Container
        if (-not $chromiumExists -or -not (Get-ChildItem $playwrightDir -Filter 'chromium*' -ErrorAction SilentlyContinue)) {
            Write-Host ""
            Write-Host "Chromium browser not found. Installing..." -ForegroundColor Yellow
            # --with-deps installs OS-level browser dependencies on Linux; on Windows it is a no-op.
            npx playwright install chromium
        }
    } finally { Pop-Location }
}

#  Run tests ─

Push-Location $e2eDir
try {
    if ($Inspect) {
        Write-Host ""
        Write-Host "Starting Playwright Inspector..." -ForegroundColor Cyan
        npx playwright test --debug
    }
    elseif ($Headed) {
        Write-Host ""
        Write-Host "Starting Playwright in headed mode..." -ForegroundColor Cyan
        npx playwright test --headed
    }
    else {
        Write-Host ""
        Write-Host "Starting Playwright tests..." -ForegroundColor Cyan
        npx playwright test
    }

    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "Tests failed. Opening report..." -ForegroundColor Red
        npx playwright show-report
        exit $LASTEXITCODE
    }

    Write-Host ""
    Write-Host "All tests passed." -ForegroundColor Green
}
finally {
    Pop-Location
}