#!/bin/bash
# Vector Development Setup Script
# Run this script to set up and start the local development environment

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# Colors
CYAN='\033[0;36m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Flags
SKIP_DOCKER=false
USE_INMEMORY=false
RUN_TESTS=false
WATCH=false

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --skip-docker) SKIP_DOCKER=true; shift ;;
        --in-memory) USE_INMEMORY=true; shift ;;
        --test) RUN_TESTS=true; shift ;;
        --watch) WATCH=true; shift ;;
        *) echo "Unknown option: $1"; exit 1 ;;
    esac
done

print_header() {
    echo -e "\n${CYAN}========================================"
    echo -e "$1"
    echo -e "========================================${NC}\n"
}

print_step() {
    echo -e "${GREEN}[*] $1${NC}"
}

print_info() {
    echo -e "    $1"
}

print_error() {
    echo -e "${RED}[!] $1${NC}"
}

# Check prerequisites
print_header "Vector Development Environment Setup"

print_step "Checking prerequisites..."
if ! command -v dotnet &> /dev/null; then
    print_error ".NET SDK not found. Please install .NET 9 SDK."
    exit 1
fi
print_info ".NET SDK: $(dotnet --version)"

if [[ "$SKIP_DOCKER" == false && "$USE_INMEMORY" == false ]]; then
    if ! command -v docker &> /dev/null; then
        print_error "Docker not found. Install Docker or use --in-memory flag."
        exit 1
    fi
    print_info "Docker: Available"
fi

# Start infrastructure if needed
if [[ "$SKIP_DOCKER" == false && "$USE_INMEMORY" == false ]]; then
    print_step "Starting infrastructure services..."
    cd "$PROJECT_ROOT"
    docker-compose -f docker-compose.infrastructure.yml up -d
    print_info "SQL Server: localhost:1433"
    print_info "Redis: localhost:6379"
    print_info "Azurite: localhost:10000-10002"
    print_info "Adminer: http://localhost:8080"
    print_info "Redis Commander: http://localhost:8081"

    print_step "Waiting for SQL Server to be ready..."
    retries=30
    ready=false
    while [[ $retries -gt 0 && "$ready" == false ]]; do
        health=$(docker inspect --format='{{.State.Health.Status}}' vector-sqlserver 2>/dev/null || echo "starting")
        if [[ "$health" == "healthy" ]]; then
            ready=true
            print_info "SQL Server is ready!"
        else
            print_info "Waiting... ($retries attempts remaining)"
            sleep 2
            ((retries--))
        fi
    done

    if [[ "$ready" == false ]]; then
        echo -e "\n${YELLOW}[!] SQL Server health check timed out, but continuing...${NC}"
        echo -e "${YELLOW}    The API will retry connections automatically.${NC}"
    fi
fi

# Restore packages
print_step "Restoring NuGet packages..."
cd "$PROJECT_ROOT"
dotnet restore --verbosity quiet

# Run tests if requested
if [[ "$RUN_TESTS" == true ]]; then
    print_step "Running tests..."
    cd "$PROJECT_ROOT"
    dotnet test --verbosity normal
fi

# Configure for in-memory if requested
if [[ "$USE_INMEMORY" == true ]]; then
    print_step "Configuring for in-memory database..."
    print_info "No external services required"
    export UseInMemoryDatabase=true
fi

# Start the API
print_header "Starting Vector API"

cd "$PROJECT_ROOT/src/Vector.Api"
print_info "Working directory: $(pwd)"
print_info "URL: http://localhost:5000"
print_info "Swagger UI: http://localhost:5000"
echo ""
print_info "Press Ctrl+C to stop"
echo ""

if [[ "$WATCH" == true ]]; then
    dotnet watch run
else
    dotnet run
fi
