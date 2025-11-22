#!/bin/bash

# Azure Authentication Setup Script
# Run this once to authenticate with Azure CLI

set -e

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

log_info() {
    echo -e "${BLUE}[$(date +'%Y-%m-%d %H:%M:%S')] [INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[$(date +'%Y-%m-%d %H:%M:%S')] [WARNING]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[$(date +'%Y-%m-%d %H:%M:%S')] [SUCCESS]${NC} $1"
}

log_info "Setting up Azure CLI authentication..."

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    log_warn "Azure CLI not found. Please rebuild the dev container."
    exit 1
fi

# Login to Azure
log_info "Logging into Azure..."
az login

# Set default subscription (optional)
log_info "Available subscriptions:"
az account list --output table

log_warn "Set default subscription? (y/N)"
read -r response
if [[ "$response" =~ ^[Yy]$ ]]; then
    echo "Enter subscription ID:"
    read -r subscription_id
    az account set --subscription "$subscription_id"
    log_info "Default subscription set to: $subscription_id"
fi

log_success "Azure authentication setup complete!"
log_info "You can now run migrations with: ./migrate.sh update"