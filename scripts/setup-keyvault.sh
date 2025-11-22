#!/bin/bash

# Azure Key Vault Setup Script
# Provisions Key Vault and uploads all application secrets

set -euo pipefail

# ----- Colors -----
declare -r RED='\033[0;31m'
declare -r GREEN='\033[0;32m'
declare -r BLUE='\033[0;34m'
declare -r YELLOW='\033[0;33m'
declare -r NC='\033[0m'

log_error()   { echo -e "${RED}[$(date +'%Y-%m-%d %H:%M:%S')] [ERROR] $*${NC}" >&2; }
log_info()    { echo -e "${BLUE}[$(date +'%Y-%m-%d %H:%M:%S')] [INFO] $*${NC}"; }
log_success() { echo -e "${GREEN}[$(date +'%Y-%m-%d %H:%M:%S')] [SUCCESS] $*${NC}"; }
log_warning() { echo -e "${YELLOW}[$(date +'%Y-%m-%d %H:%M:%S')] [WARNING] $*${NC}"; }
log_step()    { echo -e "${BLUE}[$(date +'%Y-%m-%d %H:%M:%S')] [STEP] $*${NC}"; }

# ----- Load Configuration -----
initialize_configuration() {
  local script_dir
  script_dir=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
  local project_root
  project_root=$(dirname "$script_dir")
  local config_file="${project_root}/globalenv.config"
  
  if [[ -f "$config_file" ]]; then
    # shellcheck source=/dev/null
    source "$config_file"
    log_info "Loaded configuration from: $config_file"
    return 0
  fi
  
  log_error "globalenv.config not found at: $config_file"
  exit 1
}

# ----- Validate Required Secrets -----
validate_secrets() {
  local missing_secrets=()
  
  # Required secrets
  [[ -z "${DB_CONNECTION_STRING:-}" ]] && missing_secrets+=("DB_CONNECTION_STRING")
  [[ -z "${BLOB_STORAGE_CONNECTION_STRING:-}" ]] && missing_secrets+=("BLOB_STORAGE_CONNECTION_STRING")
  [[ -z "${JWT_SECRET_KEY:-}" ]] && missing_secrets+=("JWT_SECRET_KEY")
  
  if [[ ${#missing_secrets[@]} -gt 0 ]]; then
    log_error "Missing required environment variables:"
    for secret in "${missing_secrets[@]}"; do
      log_error "  - $secret"
    done
    log_error ""
    log_error "Please set these environment variables before running the script."
    log_error "Example:"
    log_error "  export DB_CONNECTION_STRING='Server=tcp:...'"
    log_error "  export BLOB_STORAGE_CONNECTION_STRING='DefaultEndpointsProtocol=https;...'"
    log_error "  export JWT_SECRET_KEY='your-secret-key'"
    return 1
  fi
  
  return 0
}

# ----- Upload Secret Helper -----
upload_secret() {
  local keyvault_name=$1
  local secret_name=$2
  local secret_value=$3
  local tmpfile
  
  if [[ -z "$secret_value" ]]; then
    log_warning "Skipping secret '$secret_name' (not provided)"
    return 0
  fi
  
  tmpfile=$(mktemp)
  echo -n "$secret_value" > "$tmpfile"
  
  if az keyvault secret set \
    --vault-name "$keyvault_name" \
    --name "$secret_name" \
    --file "$tmpfile" \
    --output none 2>/dev/null; then
    log_info "  ✓ Uploaded: $secret_name"
  else
    log_error "  ✗ Failed to upload: $secret_name"
    rm -f "$tmpfile"
    return 1
  fi
  
  rm -f "$tmpfile"
  return 0
}

# ----- Upload Secrets to Key Vault -----
upload_secrets() {
  local keyvault_name=$1
  
  log_info "Uploading secrets to Key Vault..."
  
  # Validate required secrets
  if ! validate_secrets; then
    return 1
  fi
  
  # Required secrets
  upload_secret "$keyvault_name" "ConnectionStrings--Default" "${DB_CONNECTION_STRING}"
  upload_secret "$keyvault_name" "ConnectionStrings--BlobStorage" "${BLOB_STORAGE_CONNECTION_STRING}"
  upload_secret "$keyvault_name" "Auth--Jwt--SecretKey" "${JWT_SECRET_KEY}"
  
  # Optional secrets (only upload if provided)
  upload_secret "$keyvault_name" "Cosmos--Endpoint" "${COSMOS_ENDPOINT:-}"
  upload_secret "$keyvault_name" "Cosmos--Key" "${COSMOS_KEY:-}"
  upload_secret "$keyvault_name" "Auth--Supabase--Url" "${SUPABASE_URL:-}"
  upload_secret "$keyvault_name" "Auth--Supabase--Key" "${SUPABASE_KEY:-}"
  upload_secret "$keyvault_name" "Speech--Key" "${SPEECH_KEY:-}"
  upload_secret "$keyvault_name" "Voice--Key" "${VOICE_KEY:-}"
  upload_secret "$keyvault_name" "AzureOpenAI--ApiKey" "${AZURE_OPENAI_API_KEY:-}"
  upload_secret "$keyvault_name" "Share--WhatsApp--AccessToken" "${WHATSAPP_ACCESS_TOKEN:-}"
  
  log_success "Secrets uploaded to Key Vault"
}

# ----- Grant Container App Access to Key Vault -----
grant_container_app_access() {
  local keyvault_name=$1
  local container_app_name="${ENVIRONMENT_PREFIX}-${PROJECT_PREFIX}-worker"
  
  log_info "Granting Container App access to Key Vault"
  
  # Get Container App's managed identity principal ID
  local principal_id
  principal_id=$(az containerapp show \
    --name "$container_app_name" \
    --resource-group "$PROJECT_RESOURCE_GROUP" \
    --query "identity.principalId" -o tsv 2>/dev/null || echo "")
  
  if [[ -z "$principal_id" ]]; then
    log_info "Container App doesn't have managed identity yet. Will be configured during deployment."
    return 0
  fi
  
  # Grant Key Vault Secrets User role
  az keyvault set-policy \
    --name "$keyvault_name" \
    --object-id "$principal_id" \
    --secret-permissions get list \
    --output none
  
  log_success "Container App granted access to Key Vault"
}

# ----- Main -----
main() {
  log_info "=========================================="
  log_info "Azure Key Vault Setup Script"
  log_info "=========================================="
  
  initialize_configuration
  
  log_step "Starting Key Vault Setup"
  
  # Provision Key Vault
  local keyvault_name="${ENVIRONMENT_PREFIX}-${PROJECT_PREFIX}-kv"
  
  log_step "Checking if Key Vault exists: $keyvault_name"
  if az keyvault show --name "$keyvault_name" --resource-group "$PROJECT_RESOURCE_GROUP" &>/dev/null; then
    log_info "Key Vault already exists: $keyvault_name"
  else
    log_info "Creating Key Vault: $keyvault_name"
    az keyvault create \
      --name "$keyvault_name" \
      --resource-group "$PROJECT_RESOURCE_GROUP" \
      --location "$PROJECT_LOCATION" \
      --enable-rbac-authorization false \
      --enabled-for-deployment true \
      --enabled-for-template-deployment true \
      --output none
    log_success "Key Vault created: $keyvault_name"
  fi
  
  log_step "Uploading secrets to Key Vault: $keyvault_name"
  log_info ""
  log_info "Required environment variables:"
  log_info "  - DB_CONNECTION_STRING: SQL Database connection string"
  log_info "  - BLOB_STORAGE_CONNECTION_STRING: Azure Blob Storage connection string"
  log_info "  - JWT_SECRET_KEY: JWT signing secret key"
  log_info ""
  log_info "Optional environment variables:"
  log_info "  - COSMOS_ENDPOINT: Cosmos DB endpoint URL"
  log_info "  - COSMOS_KEY: Cosmos DB access key"
  log_info "  - SUPABASE_URL: Supabase project URL"
  log_info "  - SUPABASE_KEY: Supabase anon key"
  log_info "  - SPEECH_KEY: Azure Speech Service key"
  log_info "  - VOICE_KEY: Azure Voice Service key"
  log_info "  - AZURE_OPENAI_API_KEY: Azure OpenAI API key"
  log_info "  - WHATSAPP_ACCESS_TOKEN: Meta WhatsApp access token"
  log_info ""
  
  # Upload all secrets
  upload_secrets "$keyvault_name"
  
  # Grant Container App access (if exists)
  grant_container_app_access "$keyvault_name"
  
  log_info "=========================================="
  log_success "Key Vault setup completed successfully!"
  log_info "=========================================="
  log_info "Key Vault Name: $keyvault_name"
  log_info "Key Vault URI: https://${keyvault_name}.vault.azure.net/"
}

main "$@"
