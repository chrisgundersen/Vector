# Vector Development Setup Script
# Run this script to set up and start the local development environment

param(
    [switch]$SkipDocker,
    [switch]$UseInMemory,
    [switch]$RunTests,
    [switch]$Watch
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if (-not $ProjectRoot) { $ProjectRoot = (Get-Location).Path }

function Write-Header {
    param([string]$Message)
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host $Message -ForegroundColor Cyan
    Write-Host "========================================`n" -ForegroundColor Cyan
}

function Write-Step {
    param([string]$Message)
    Write-Host "[*] $Message" -ForegroundColor Green
}

function Write-Info {
    param([string]$Message)
    Write-Host "    $Message" -ForegroundColor Gray
}

function Write-Error {
    param([string]$Message)
    Write-Host "[!] $Message" -ForegroundColor Red
}

# Check prerequisites
Write-Header "Vector Development Environment Setup"

Write-Step "Checking prerequisites..."
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error ".NET SDK not found. Please install .NET 9 SDK."
    exit 1
}
Write-Info "  .NET SDK: $(dotnet --version)"

if (-not $SkipDocker -and -not $UseInMemory) {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        Write-Error "Docker not found. Install Docker Desktop or use -UseInMemory flag."
        exit 1
    }
    Write-Info "  Docker: Available"
}

# Start infrastructure if needed
if (-not $SkipDocker -and -not $UseInMemory) {
    Write-Step "Starting infrastructure services..."
    Push-Location $ProjectRoot
    try {
        docker-compose -f docker-compose.infrastructure.yml up -d
        Write-Info "  SQL Server: localhost:1433"
        Write-Info "  Redis: localhost:6379"
        Write-Info "  Azurite: localhost:10000-10002"
        Write-Info "  Adminer: http://localhost:8080"
        Write-Info "  Redis Commander: http://localhost:8081"

        Write-Step "Waiting for SQL Server to be ready..."
        $retries = 30
        $ready = $false
        while ($retries -gt 0 -and -not $ready) {
            try {
                $health = docker inspect --format='{{.State.Health.Status}}' vector-sqlserver 2>$null
                if ($health -eq "healthy") {
                    $ready = $true
                    Write-Info "  SQL Server is ready!"
                } else {
                    Write-Info "  Waiting... ($retries attempts remaining)"
                    Start-Sleep -Seconds 2
                    $retries--
                }
            } catch {
                Start-Sleep -Seconds 2
                $retries--
            }
        }

        if (-not $ready) {
            Write-Host "`n[!] SQL Server health check timed out, but continuing..." -ForegroundColor Yellow
            Write-Host "    The API will retry connections automatically." -ForegroundColor Yellow
        }
    } finally {
        Pop-Location
    }
}

# Restore packages
Write-Step "Restoring NuGet packages..."
Push-Location $ProjectRoot
try {
    dotnet restore --verbosity quiet
} finally {
    Pop-Location
}

# Run tests if requested
if ($RunTests) {
    Write-Step "Running tests..."
    Push-Location $ProjectRoot
    try {
        dotnet test --verbosity normal
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Tests failed!"
            exit 1
        }
    } finally {
        Pop-Location
    }
}

# Configure for in-memory if requested
if ($UseInMemory) {
    Write-Step "Configuring for in-memory database..."
    Write-Info "  No external services required"
}

# Start the API
Write-Header "Starting Vector API"

$apiPath = Join-Path $ProjectRoot "src\Vector.Api"
Write-Info "Working directory: $apiPath"
Write-Info "URL: http://localhost:5000"
Write-Info "Swagger UI: http://localhost:5000"
Write-Info ""
Write-Info "Press Ctrl+C to stop"
Write-Host ""

Push-Location $apiPath
try {
    if ($UseInMemory) {
        $env:UseInMemoryDatabase = "true"
    }

    if ($Watch) {
        dotnet watch run
    } else {
        dotnet run
    }
} finally {
    Pop-Location
}
