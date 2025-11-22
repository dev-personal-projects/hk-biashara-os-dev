#!/bin/bash

# Entity Framework Migration Script
# Usage: ./migrate.sh [add|update|remove|list] [migration_name]

set -e

# Configuration
PROJECT_DIR="src/ApiWorker"
EF_TOOL="/root/.dotnet/tools/dotnet-ef"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Helper functions
log_info() {
    echo -e "${BLUE}[$(date +'%Y-%m-%d %H:%M:%S')] [INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[$(date +'%Y-%m-%d %H:%M:%S')] [WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[$(date +'%Y-%m-%d %H:%M:%S')] [ERROR]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[$(date +'%Y-%m-%d %H:%M:%S')] [SUCCESS]${NC} $1"
}

# Check if EF tools are installed
check_ef_tools() {
    if [ ! -f "$EF_TOOL" ]; then
        log_warn "EF Core tools not found. Installing..."
        dotnet tool install --global dotnet-ef --version 9.0.0
        export PATH="$PATH:/root/.dotnet/tools"
    fi
}

# Navigate to project directory
cd "$PROJECT_DIR" || {
    log_error "Project directory not found: $PROJECT_DIR"
    exit 1
}

# Check EF tools
check_ef_tools

# Main command handling
case "$1" in
    "add")
        if [ -z "$2" ]; then
            log_error "Migration name required. Usage: ./migrate.sh add <migration_name>"
            exit 1
        fi
        log_info "Creating migration: $2"
        $EF_TOOL migrations add "$2"
        log_success "Migration '$2' created successfully"
        ;;
    
    "update")
        log_info "Applying migrations to database..."
        $EF_TOOL database update
        log_success "Database updated successfully"
        ;;
    
    "remove")
        log_info "Removing last migration..."
        $EF_TOOL migrations remove
        log_success "Last migration removed successfully"
        ;;
    
    "list")
        log_info "Listing migrations..."
        $EF_TOOL migrations list
        ;;
    
    "reset")
        if [ -z "$2" ]; then
            log_error "Target migration required. Usage: ./migrate.sh reset <migration_name>"
            exit 1
        fi
        log_info "Rolling back to migration: $2"
        $EF_TOOL database update "$2"
        log_info "Database rolled back to '$2'"
        ;;
    
    "fresh")
        log_warn "This will drop and recreate the database. Continue? (y/N)"
        read -r response
        if [[ "$response" =~ ^[Yy]$ ]]; then
            log_info "Dropping database..."
            $EF_TOOL database drop --force
            log_info "Applying all migrations..."
            $EF_TOOL database update
            log_success "Database recreated successfully"
        else
            log_info "Operation cancelled"
        fi
        ;;
    
    "clean")
        log_warn "This will remove all migration files. Continue? (y/N)"
        read -r response
        if [[ "$response" =~ ^[Yy]$ ]]; then
            log_info "Removing all migration files..."
            rm -rf Migrations/*
            log_info "All migrations removed. Run 'add' to create initial migration."
        else
            log_info "Operation cancelled"
        fi
        ;;
    
    *)
        echo "Entity Framework Migration Helper"
        echo ""
        echo "Usage: ./migrate.sh <command> [options]"
        echo ""
        echo "Commands:"
        echo "  add <name>     Create a new migration"
        echo "  update         Apply pending migrations to database"
        echo "  remove         Remove the last migration"
        echo "  list           List all migrations"
        echo "  reset <name>   Roll back to specific migration"
        echo "  fresh          Drop and recreate database (destructive)"
        echo "  clean          Remove all migration files (destructive)"
        echo ""
        echo "Examples:"
        echo "  ./migrate.sh add AddUserTable"
        echo "  ./migrate.sh update"
        echo "  ./migrate.sh list"
        exit 1
        ;;
esac