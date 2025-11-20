#!/bin/bash

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"

log() {
    local level=$1
    shift
    printf '[%s] %s\n' "$level" "$*"
}

log "INFO" "Working directory: $ROOT_DIR"
log "INFO" "ASPNETCORE_ENVIRONMENT=${ENVIRONMENT}"

cd "$ROOT_DIR"

if ! command -v dotnet >/dev/null 2>&1; then
    log "ERROR" ".NET SDK is not installed or not in PATH"
    exit 1
fi

log "INFO" "Restoring and running template seeder..."
dotnet run --project scripts/SeedTemplates.csproj --configuration Release

log "INFO" "Template seeding completed."