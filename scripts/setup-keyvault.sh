#!/bin/bash

# Azure Key Vault Setup Script
# Provisions Key Vault and uploads all application secrets

set -euo pipefail

# ----- Colors -----
declare -r RED='\033[0;31m'
declare -r GREEN='\033[0;32m'
declare -r BLUE='\033[0;34m'
declare -r NC='\033[0m'

log_error()   { echo -e "${RED}[ERROR] $*${NC}" >&2; }
log_info()    { echo -e "${BLUE}[INFO] $*${NC}"; }
log_success() { echo -e "${GREEN}[SUCCESS] $*${NC}"; }

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

# ----- Upload Secrets to Key Vault -----
upload_secrets() {
  local keyvault_name=$1
  local tmpfile=$(mktemp)
  
  # Connection Strings
  cat > "$tmpfile" << 'EOF'
Server=tcp:biashara-os-server-name.database.windows.net,1433;Initial Catalog=biashara-os-db-name;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication="Active Directory Default";
EOF
  az keyvault secret set --vault-name "$keyvault_name" --name "ConnectionStrings--Default" --file "$tmpfile" --output none
  
  cat > "$tmpfile" << 'EOF'
DefaultEndpointsProtocol=https;AccountName=biasharaos;AccountKey=FpuEloIliwJrI3NrIl6jan6GxW+jVejUuiaPuOF9UkrwuGEueIkMM85FePBJleuT9woLeQMDTi4b+AStvvHwbw==;EndpointSuffix=core.windows.net
EOF
  az keyvault secret set --vault-name "$keyvault_name" --name "ConnectionStrings--BlobStorage" --file "$tmpfile" --output none
  
  # Cosmos DB
  echo -n 'https://biasharaops-accout.documents.azure.com:443/' > "$tmpfile"
  az keyvault secret set --vault-name "$keyvault_name" --name "Cosmos--Endpoint" --file "$tmpfile" --output none
  
  echo -n 'Apa97fA0OheDOKdR1OV4W9isKrU7kWZnubALKC6dY02HfWjldBImuVzotu3InflUdVi6Oq147uOUACDbiJO24w==' > "$tmpfile"
  az keyvault secret set --vault-name "$keyvault_name" --name "Cosmos--Key" --file "$tmpfile" --output none
  
  # Supabase Auth
  echo -n 'https://pqsnqrovyidkpcfmfwec.supabase.co' > "$tmpfile"
  az keyvault secret set --vault-name "$keyvault_name" --name "Auth--Supabase--Url" --file "$tmpfile" --output none
  
  echo -n 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InBxc25xcm92eWlka3BjZm1md2VjIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjE0NjEzNTMsImV4cCI6MjA3NzAzNzM1M30.Dc34BqSGaIUck6LwY_fXCn0AfIohEuMZqHmjSvH7lrw' > "$tmpfile"
  az keyvault secret set --vault-name "$keyvault_name" --name "Auth--Supabase--Key" --file "$tmpfile" --output none
  
  # JWT Secret
  echo -n 'ZiWI5kVr3qmnEppSfAoKBUgeNbbg6diSfA/dpKB2vOdnKh1vQl6UQ/OK/6m2qaAe5Rgejrzn0KsPt5XmyDyU3g==' > "$tmpfile"
  az keyvault secret set --vault-name "$keyvault_name" --name "Auth--Jwt--SecretKey" --file "$tmpfile" --output none
  
  # Azure Speech
  echo -n 'nWY5WZsOKi68uZDdiZsTO7HXfXCXropGlW3p2WFmFrBITiM6qVa4JQQJ99BKACi5YpzXJ3w3AAAYACOGi57R' > "$tmpfile"
  az keyvault secret set --vault-name "$keyvault_name" --name "Speech--Key" --file "$tmpfile" --output none
  
  echo -n 'nWY5WZsOKi68uZDdiZsTO7HXfXCXropGlW3p2WFmFrBITiM6qVa4JQQJ99BKACi5YpzXJ3w3AAAYACOGi57R' > "$tmpfile"
  az keyvault secret set --vault-name "$keyvault_name" --name "Voice--Key" --file "$tmpfile" --output none
  
  # Azure OpenAI
  echo -n 'your-aoai-key' > "$tmpfile"
  az keyvault secret set --vault-name "$keyvault_name" --name "AzureOpenAI--ApiKey" --file "$tmpfile" --output none
  
  # WhatsApp
  echo -n 'meta-access-token' > "$tmpfile"
  az keyvault secret set --vault-name "$keyvault_name" --name "Share--WhatsApp--AccessToken" --file "$tmpfile" --output none
  
  rm -f "$tmpfile"
  log_success "All secrets uploaded to Key Vault"
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
  initialize_configuration
  
  log_info "Starting Key Vault Setup"
  
  # Provision Key Vault
  local keyvault_name="${ENVIRONMENT_PREFIX}-${PROJECT_PREFIX}-kv"
  
  log_info "Checking if Key Vault exists: $keyvault_name"
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
  
  log_info "Uploading secrets to Key Vault: $keyvault_name"
  
  # Upload all secrets
  upload_secrets "$keyvault_name"
  
  # Grant Container App access (if exists)
  grant_container_app_access "$keyvault_name"
  
  log_success "Key Vault setup completed successfully"
  log_info "Key Vault Name: $keyvault_name"
  log_info "Key Vault URI: https://${keyvault_name}.vault.azure.net/"
}

main "$@"
