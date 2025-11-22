#!/bin/bash

# Enhanced Deployment Script for Azure Container Apps with Robust Error Handling and Logging

# Strict mode for better error handling
set -euo pipefail

# ----- Colors -----
declare -r YELLOW='\033[0;33m'
declare -r RED='\033[0;31m'
declare -r GREEN='\033[0;32m'
declare -r BLUE='\033[0;34m'
declare -r NC='\033[0m'

# ----- Logging -----
log_error()   { echo -e "${RED}[$(date +'%Y-%m-%d %H:%M:%S')] [ERROR] $*${NC}" >&2; }
log_info()    { echo -e "${BLUE}[$(date +'%Y-%m-%d %H:%M:%S')] [INFO] $*${NC}"; }
log_success() { echo -e "${GREEN}[$(date +'%Y-%m-%d %H:%M:%S')] [SUCCESS] $*${NC}"; }
log_warning() { echo -e "${YELLOW}[$(date +'%Y-%m-%d %H:%M:%S')] [WARNING] $*${NC}"; }
log_step()    { echo -e "${BLUE}[$(date +'%Y-%m-%d %H:%M:%S')] [STEP] $*${NC}"; }

# ----- Error Trap -----
handle_error() {
  local line_number=$1
  local command=$2
  log_error "Error occurred at line $line_number: $command"
  exit 1
}
trap 'handle_error $LINENO "$BASH_COMMAND"' ERR

# ----- Init Config -----
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

# ----- Validate Env -----
validate_configuration() {
  log_step "Validating configuration..."
  local required_vars=(
    "ENVIRONMENT_PREFIX"
    "PROJECT_PREFIX"
    "PROJECT_LOCATION"
    "LOG_FOLDER"
    "PROJECT_RESOURCE_GROUP"
    "PROJECT_SUBSCRIPTION_ID"
  )
  for var in "${required_vars[@]}"; do
    if [[ -z "${!var:-}" ]]; then
      log_error "Required environment variable $var is not set"
      return 1
    fi
  done
  
  # Set defaults for optional variables
  REPOSITORY_URL="${REPOSITORY_URL:-https://github.com/dev-personal-projects/hk-biashara-os-dev}"
  GIT_BRANCH="${GIT_BRANCH:-dev}"
  TARGET_PORT="${TARGET_PORT:-8080}"
  
  log_info "Configuration validated successfully"
  log_info "  Environment: ${ENVIRONMENT_PREFIX}"
  log_info "  Resource Group: ${PROJECT_RESOURCE_GROUP}"
  log_info "  Location: ${PROJECT_LOCATION}"
  log_info "  Repository: ${REPOSITORY_URL}"
  log_info "  Branch: ${GIT_BRANCH}"
  log_info "  Target Port: ${TARGET_PORT}"
}

# ----- Azure Context -----
setup_azure_context() {
  log_info "Checking Azure CLI authentication"
  if ! az account show &>/dev/null; then
    log_warning "Not logged in to Azure CLI. Initiating login..."
    az login
  fi

  log_info "Setting Azure subscription to ${PROJECT_SUBSCRIPTION_ID}"
  az account set --subscription "${PROJECT_SUBSCRIPTION_ID}"
  local current_subscription
  current_subscription=$(az account show --query id -o tsv)
  if [[ "$current_subscription" != "$PROJECT_SUBSCRIPTION_ID" ]]; then
    log_error "Failed to set Azure subscription. Current: $current_subscription, Expected: $PROJECT_SUBSCRIPTION_ID"
    return 1
  fi
}

# ----- Ensure Resource Group -----
ensure_resource_group() {
  if ! az group show -n "$PROJECT_RESOURCE_GROUP" &>/dev/null; then
    log_warning "Resource group '$PROJECT_RESOURCE_GROUP' not found. Creating..."
    az group create -n "$PROJECT_RESOURCE_GROUP" -l "$PROJECT_LOCATION" --output none >/dev/null 2>&1
    log_success "Resource group created"
  else
    log_info "Resource group exists: $PROJECT_RESOURCE_GROUP"
  fi
}

# ----- Ensure Bicep + Container Apps extension -----
ensure_cli_prereqs() {
  if ! az bicep version &>/dev/null; then
    log_info "Installing Azure Bicep CLI..."
    az bicep install >/dev/null
  fi
  if ! az extension show --name containerapp &>/dev/null; then
    log_info "Installing Azure 'containerapp' extension..."
    az extension add --name containerapp >/dev/null
  fi
  
  # Check for jq (optional but recommended for better JSON parsing)
  if ! command -v jq &> /dev/null; then
    log_warning "jq is not installed. The script will use basic JSON parsing."
    log_info "To install jq for better reliability: sudo apt-get update && sudo apt-get install -y jq"
    log_info "Or on Alpine: apk add jq"
  fi
}

# ----- Provision Infra with Bicep (idempotent) -----
provision_infra_bicep() {
  log_step "Provisioning infrastructure with Bicep..."
  local script_dir
  script_dir=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
  local project_root
  project_root=$(dirname "$script_dir")
  local bicep_dir="${project_root}/bicep"
  local template_file="${bicep_dir}/main.bicep"
  local params_file="${bicep_dir}/parameters.${ENVIRONMENT_PREFIX}.json"

  if [[ ! -f "$template_file" ]]; then
    log_error "Bicep template not found at: $template_file"
    exit 1
  fi

  log_info "Deploying infrastructure via Bicep (template: $template_file)"
  local deploy_result=0
  local deploy_output=""
  
  if [[ -f "$params_file" ]]; then
    log_info "Using parameters file: $params_file"
    log_info "Provisioning resources (this may take 1-2 minutes)..."
    deploy_output=$(az deployment group create \
      --resource-group "$PROJECT_RESOURCE_GROUP" \
      --template-file "$template_file" \
      --parameters @"$params_file" \
      --output none 2>&1) || deploy_result=$?
  else
    log_warning "Parameters file not found at ${params_file}. Using inline parameters."
    log_info "Provisioning resources (this may take 1-2 minutes)..."
    deploy_output=$(az deployment group create \
      --resource-group "$PROJECT_RESOURCE_GROUP" \
      --template-file "$template_file" \
      --parameters environment="$ENVIRONMENT_PREFIX" \
                   repositoryUrl="${REPOSITORY_URL}" \
                   branch="${GIT_BRANCH}" \
                   location="$PROJECT_LOCATION" \
                   cpu="0.25" \
                   memory="0.5Gi" \
                   scaleMin=1 \
                   scaleMax=10 \
      --output none 2>&1) || deploy_result=$?
  fi
  
  # Filter out verbose messages but keep errors
  local filtered_deploy_output
  filtered_deploy_output=$(echo "$deploy_output" | grep -v "^INFO:" | grep -v "^WARNING:" || true)
  
  if [[ $deploy_result -eq 0 ]]; then
    log_success "Bicep deployment completed successfully"
  else
    log_error "Bicep deployment failed (exit code: $deploy_result)"
    if [[ -n "$filtered_deploy_output" ]]; then
      log_error "Deployment error details:"
      echo "$filtered_deploy_output" | grep -iE "error|failed|exception" | head -10 | sed 's/^/  /'
    fi
    return 1
  fi
}

# ----- Prepare ACR -----
prepare_container_registry() {
  local registry_name="${ENVIRONMENT_PREFIX}${PROJECT_PREFIX}contregistry"
  log_step "Checking Azure Container Registry: $registry_name"

  if ! az acr show --name "$registry_name" --output none &>/dev/null; then
    log_warning "Container Registry does not exist. Creating..."
    if az acr create \
      --name "$registry_name" \
      --resource-group "$PROJECT_RESOURCE_GROUP" \
      --sku Basic \
      --admin-enabled true \
      --output none 2>/dev/null; then
      log_success "Container Registry created: $registry_name"
    else
      log_error "Failed to create Container Registry"
      return 1
    fi
  else
    log_info "Container Registry exists: $registry_name"
  fi

  log_info "Logging into Container Registry..."
  if az acr login --name "$registry_name" --output none 2>/dev/null; then
    log_success "Successfully logged into Container Registry"
  else
    log_warning "ACR login may have failed, but continuing..."
  fi
}

# ----- Get Container Apps Environment Name from Bicep -----
get_container_apps_environment_name() {
  # Bicep creates environments with pattern: env-${environment}-${uniqueSuffix}
  # We need to query for the environment created by Bicep
  local env_pattern="env-${ENVIRONMENT_PREFIX}-"
  
  log_step "Finding Container Apps Environment created by Bicep..." >&2
  
  # Query for environments matching the Bicep pattern
  # Redirect stderr to avoid capturing log messages in the output
  local env_name
  env_name=$(az containerapp env list \
    --resource-group "$PROJECT_RESOURCE_GROUP" \
    --query "[?starts_with(name, '${env_pattern}')].name" -o tsv 2>/dev/null | head -1)
  
  if [[ -n "$env_name" ]]; then
    log_info "Found Container Apps Environment: $env_name" >&2
    # Output only the environment name to stdout (for capture)
    echo "$env_name"
    return 0
  else
    log_warning "Container Apps Environment matching pattern '${env_pattern}*' not found." >&2
    log_info "This is expected if Bicep deployment just completed - environment may still be provisioning." >&2
    log_info "The environment will be created by Bicep or az containerapp up will handle it." >&2
    return 1
  fi
}

# ----- Prepare Container Apps Environment -----
prepare_container_apps_environment() {
  # Try to get environment name from Bicep deployment
  local environment_name
  environment_name=$(get_container_apps_environment_name) || environment_name=""
  
  # If not found, log that Bicep will create it or az containerapp up will handle it
  if [[ -z "$environment_name" ]]; then
    log_info "Container Apps Environment will be managed by Bicep or az containerapp up"
    log_info "No manual environment preparation needed"
  fi
  
  local container_app_name="${ENVIRONMENT_PREFIX}-${PROJECT_PREFIX}-worker"
  local registry_url="${ENVIRONMENT_PREFIX}${PROJECT_PREFIX}contregistry.azurecr.io"

  log_info "Deployment Targets:"
  if [[ -n "$environment_name" ]]; then
    log_info "  Environment: $environment_name"
  else
    log_info "  Environment: (will be created/identified during deployment)"
  fi
  log_info "  Container App: $container_app_name"
  log_info "  Registry: $registry_url"
}

# ----- Create or Get Service Principal -----
create_or_get_service_principal() {
  log_step "Setting up Service Principal for GitHub Actions..."
  local sp_name="${ENVIRONMENT_PREFIX}-${PROJECT_PREFIX}-github-sp"
  local sp_output
  
  # Check if service principal already exists
  if SERVICE_PRINCIPAL_CLIENT_ID=$(az ad sp list --display-name "$sp_name" --query "[0].appId" -o tsv 2>/dev/null) && [[ -n "$SERVICE_PRINCIPAL_CLIENT_ID" ]]; then
    log_info "Service Principal '$sp_name' already exists (Client ID: $SERVICE_PRINCIPAL_CLIENT_ID)"
    SERVICE_PRINCIPAL_TENANT_ID=$(az account show --query tenantId -o tsv)
    
    # Check if we have the secret in config
    if [[ -z "${SERVICE_PRINCIPAL_CLIENT_SECRET:-}" ]]; then
      log_warning "Service Principal exists but secret is not available in config."
      log_info "Automatically resetting Service Principal credentials..."
      
      # Reset credentials to get a new secret
      local temp_file
      temp_file=$(mktemp)
      local reset_output
      
      # Reset credentials - capture stderr separately to avoid mixing with JSON
      local reset_stderr
      reset_stderr=$(mktemp)
      if reset_output=$(az ad sp credential reset --id "$SERVICE_PRINCIPAL_CLIENT_ID" --output json 2>"$reset_stderr"); then
        # Check if output is valid JSON (starts with {)
        if [[ "$reset_output" =~ ^\{ ]]; then
          echo "$reset_output" > "$temp_file"
          
          # Extract the new password/secret
          if command -v jq &> /dev/null; then
            SERVICE_PRINCIPAL_CLIENT_SECRET=$(jq -r '.password // empty' "$temp_file" 2>/dev/null)
          else
            # Fallback: use sed for basic JSON parsing
            SERVICE_PRINCIPAL_CLIENT_SECRET=$(sed -n 's/.*"password"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' "$temp_file" 2>/dev/null)
          fi
        else
          log_warning "Service principal reset output is not valid JSON. Output: ${reset_output:0:100}"
          # Try to extract from stderr or use Azure CLI query
          if [[ -s "$reset_stderr" ]]; then
            log_warning "Error details: $(cat "$reset_stderr")"
          fi
        fi
        
        rm -f "$temp_file" "$reset_stderr"
        
        if [[ -n "$SERVICE_PRINCIPAL_CLIENT_SECRET" ]] && [[ "$SERVICE_PRINCIPAL_CLIENT_SECRET" != "null" ]]; then
          log_success "Service Principal credentials reset successfully"
          log_info "New secret has been generated and will be used for this deployment"
          
          # Save to GitHub secrets if gh CLI is available
          if command -v gh &> /dev/null; then
            log_step "Saving updated Service Principal credentials to GitHub Secrets..."
            if gh auth status &>/dev/null; then
              gh secret set AZURE_CLIENT_ID --body "$SERVICE_PRINCIPAL_CLIENT_ID" 2>/dev/null && log_success "Updated AZURE_CLIENT_ID in GitHub Secrets" || log_warning "Failed to update AZURE_CLIENT_ID"
              gh secret set AZURE_CLIENT_SECRET --body "$SERVICE_PRINCIPAL_CLIENT_SECRET" 2>/dev/null && log_success "Updated AZURE_CLIENT_SECRET in GitHub Secrets" || log_warning "Failed to update AZURE_CLIENT_SECRET"
              gh secret set AZURE_TENANT_ID --body "$SERVICE_PRINCIPAL_TENANT_ID" 2>/dev/null && log_success "Updated AZURE_TENANT_ID in GitHub Secrets" || log_warning "Failed to update AZURE_TENANT_ID"
            else
              log_warning "GitHub CLI not authenticated. Run 'gh auth login' to enable automatic secret storage."
              log_info "Please manually update GitHub Secrets with the new credentials:"
              log_info "  AZURE_CLIENT_ID: $SERVICE_PRINCIPAL_CLIENT_ID"
              log_info "  AZURE_CLIENT_SECRET: $SERVICE_PRINCIPAL_CLIENT_SECRET"
              log_info "  AZURE_TENANT_ID: $SERVICE_PRINCIPAL_TENANT_ID"
            fi
          else
            log_warning "GitHub CLI (gh) not found. Please manually update GitHub Secrets:"
            log_info "  AZURE_CLIENT_ID: $SERVICE_PRINCIPAL_CLIENT_ID"
            log_info "  AZURE_CLIENT_SECRET: $SERVICE_PRINCIPAL_CLIENT_SECRET"
            log_info "  AZURE_TENANT_ID: $SERVICE_PRINCIPAL_TENANT_ID"
          fi
        else
          log_warning "Failed to extract new secret from reset output."
          log_info "The script will attempt to use ACR admin credentials as fallback."
          SERVICE_PRINCIPAL_CLIENT_SECRET=""
        fi
      else
        log_warning "Failed to reset Service Principal credentials."
        if [[ -s "$reset_stderr" ]]; then
          log_warning "Error: $(cat "$reset_stderr")"
        fi
        rm -f "$temp_file" "$reset_stderr"
        log_info "The script will attempt to use ACR admin credentials as fallback."
        SERVICE_PRINCIPAL_CLIENT_SECRET=""
      fi
    else
      log_info "Using Service Principal credentials from configuration"
    fi
    return 0
  fi
  
  # Create new service principal
  log_info "Creating new Service Principal: $sp_name"
  
  # Create service principal - password is only returned once during creation
  # Capture JSON output to temporary file for parsing
  local temp_file
  temp_file=$(mktemp)
  local create_output
  
  if ! create_output=$(az ad sp create-for-rbac \
    --name "$sp_name" \
    --role "Contributor" \
    --scopes "/subscriptions/${PROJECT_SUBSCRIPTION_ID}/resourceGroups/${PROJECT_RESOURCE_GROUP}" \
    --output json 2>&1); then
    log_error "Failed to create service principal. Error: $create_output"
    return 1
  fi
  
  # Save output to temp file for parsing
  echo "$create_output" > "$temp_file"
  
  # Extract values - use jq if available, otherwise use sed
  if command -v jq &> /dev/null; then
    # Use jq for reliable JSON parsing
    SERVICE_PRINCIPAL_CLIENT_ID=$(jq -r '.appId' "$temp_file")
    SERVICE_PRINCIPAL_CLIENT_SECRET=$(jq -r '.password' "$temp_file")
    SERVICE_PRINCIPAL_TENANT_ID=$(jq -r '.tenant' "$temp_file")
  else
    # Fallback: use sed for basic JSON parsing (works for simple JSON)
    SERVICE_PRINCIPAL_CLIENT_ID=$(sed -n 's/.*"appId"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' "$temp_file")
    SERVICE_PRINCIPAL_CLIENT_SECRET=$(sed -n 's/.*"password"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' "$temp_file")
    SERVICE_PRINCIPAL_TENANT_ID=$(sed -n 's/.*"tenant"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' "$temp_file")
  fi
  
  # Fallback: use Azure CLI query if grep/sed failed
  if [[ -z "$SERVICE_PRINCIPAL_CLIENT_ID" ]]; then
    SERVICE_PRINCIPAL_CLIENT_ID=$(az ad sp list --display-name "$sp_name" --query "[0].appId" -o tsv 2>/dev/null)
  fi
  
  if [[ -z "$SERVICE_PRINCIPAL_TENANT_ID" ]]; then
    SERVICE_PRINCIPAL_TENANT_ID=$(az account show --query tenantId -o tsv)
  fi
  
  rm -f "$temp_file"
  
  # Validate we got all required values
  if [[ -z "$SERVICE_PRINCIPAL_CLIENT_ID" ]] || [[ -z "$SERVICE_PRINCIPAL_CLIENT_SECRET" ]] || [[ -z "$SERVICE_PRINCIPAL_TENANT_ID" ]]; then
    log_error "Failed to extract service principal credentials."
    log_error "Client ID: ${SERVICE_PRINCIPAL_CLIENT_ID:-<empty>}"
    log_error "Client Secret: ${SERVICE_PRINCIPAL_CLIENT_SECRET:+<set>}${SERVICE_PRINCIPAL_CLIENT_SECRET:-<empty>}"
    log_error "Tenant ID: ${SERVICE_PRINCIPAL_TENANT_ID:-<empty>}"
    log_error "Please create service principal manually or install 'jq' for better JSON parsing."
    return 1
  fi
  
  log_success "Service Principal '$sp_name' created successfully"
  log_info "  Client ID: $SERVICE_PRINCIPAL_CLIENT_ID"
  log_info "  Tenant ID: $SERVICE_PRINCIPAL_TENANT_ID"
  
  # Save to GitHub secrets if gh CLI is available
  if command -v gh &> /dev/null; then
    log_step "Saving Service Principal credentials to GitHub Secrets..."
    if gh auth status &>/dev/null; then
      gh secret set AZURE_CLIENT_ID --body "$SERVICE_PRINCIPAL_CLIENT_ID" 2>/dev/null && log_success "Saved AZURE_CLIENT_ID to GitHub Secrets" || log_warning "Failed to save AZURE_CLIENT_ID"
      gh secret set AZURE_CLIENT_SECRET --body "$SERVICE_PRINCIPAL_CLIENT_SECRET" 2>/dev/null && log_success "Saved AZURE_CLIENT_SECRET to GitHub Secrets" || log_warning "Failed to save AZURE_CLIENT_SECRET"
      gh secret set AZURE_TENANT_ID --body "$SERVICE_PRINCIPAL_TENANT_ID" 2>/dev/null && log_success "Saved AZURE_TENANT_ID to GitHub Secrets" || log_warning "Failed to save AZURE_TENANT_ID"
    else
      log_warning "GitHub CLI not authenticated. Run 'gh auth login' to enable automatic secret storage."
      log_info "Manual GitHub Secrets to add:"
      log_info "  AZURE_CLIENT_ID: $SERVICE_PRINCIPAL_CLIENT_ID"
      log_info "  AZURE_CLIENT_SECRET: $SERVICE_PRINCIPAL_CLIENT_SECRET"
      log_info "  AZURE_TENANT_ID: $SERVICE_PRINCIPAL_TENANT_ID"
    fi
  else
    log_warning "GitHub CLI (gh) not found. Please manually add these secrets to GitHub:"
    log_info "  AZURE_CLIENT_ID: $SERVICE_PRINCIPAL_CLIENT_ID"
    log_info "  AZURE_CLIENT_SECRET: $SERVICE_PRINCIPAL_CLIENT_SECRET"
    log_info "  AZURE_TENANT_ID: $SERVICE_PRINCIPAL_TENANT_ID"
  fi
}

# ----- Build & Deploy Container App -----
deploy_container_app() {
  log_step "Deploying Container App..."
  
  # Get the environment name created by Bicep (pattern: env-${environment}-${uniqueSuffix})
  # Capture only stdout (environment name), redirect stderr (log messages) to /dev/null
  local environment_name
  environment_name=$(get_container_apps_environment_name 2>/dev/null) || {
    log_warning "Could not find Container Apps Environment from Bicep deployment."
    log_info "az containerapp up will create or use the appropriate environment."
    # Set to empty - az containerapp up will handle environment selection
    environment_name=""
  }
  
  # Clean up environment name (remove any whitespace or newlines)
  environment_name=$(echo "$environment_name" | tr -d '\n\r' | xargs)
  
  local container_app_name="${ENVIRONMENT_PREFIX}-${PROJECT_PREFIX}-worker"
  local registry_url="${ENVIRONMENT_PREFIX}${PROJECT_PREFIX}contregistry.azurecr.io"
  local repo_url="${REPOSITORY_URL}"
  local branch="${GIT_BRANCH}"
  # Key Vault name - can be overridden by KEY_VAULT_NAME environment variable
  local keyvault_name="${KEY_VAULT_NAME:-${ENVIRONMENT_PREFIX}-${PROJECT_PREFIX}-kv}"
  local target_port="${TARGET_PORT}"
  
  # Get service principal credentials from config or create new
  if [[ -z "${SERVICE_PRINCIPAL_CLIENT_ID:-}" ]] || [[ -z "${SERVICE_PRINCIPAL_TENANT_ID:-}" ]]; then
    log_warning "Service Principal credentials not found in config. Creating new Service Principal..."
    create_or_get_service_principal || {
      log_error "Failed to create/get Service Principal"
      return 1
    }
  else
    log_info "Using Service Principal from configuration"
  fi
  
  local client_id="${SERVICE_PRINCIPAL_CLIENT_ID}"
  local client_secret="${SERVICE_PRINCIPAL_CLIENT_SECRET:-}"
  local tenant_id="${SERVICE_PRINCIPAL_TENANT_ID}"
  
  # If service principal secret is missing, try to use ACR admin credentials
  local use_acr_admin=false
  local acr_username=""
  local acr_password=""
  
  if [[ -z "$client_secret" ]]; then
    log_warning "Service Principal secret not available. Attempting to use ACR admin credentials..."
    local registry_name="${ENVIRONMENT_PREFIX}${PROJECT_PREFIX}contregistry"
    acr_username=$(az acr credential show --name "$registry_name" --query username -o tsv 2>/dev/null || echo "")
    acr_password=$(az acr credential show --name "$registry_name" --query passwords[0].value -o tsv 2>/dev/null || echo "")
    
    if [[ -n "$acr_username" && -n "$acr_password" ]]; then
      log_info "Using ACR admin credentials for registry authentication"
      use_acr_admin=true
    else
      log_error "Cannot proceed: Service Principal secret is missing and ACR admin credentials unavailable."
      log_error "Please either:"
      log_error "  1. Add SERVICE_PRINCIPAL_CLIENT_SECRET to globalenv.config"
      log_error "  2. Reset SP credentials: az ad sp credential reset --id $client_id"
      log_error "  3. Enable ACR admin: az acr update --name $registry_name --admin-enabled true"
      return 1
    fi
  fi

  log_info "Deployment Configuration:"
  log_info "  Container App: $container_app_name"
  log_info "  Environment: $environment_name"
  log_info "  Registry: $registry_url"
  log_info "  Repository: $repo_url"
  log_info "  Branch: $branch"
  log_info "  Target Port: $target_port"
  if [[ "$use_acr_admin" == true ]]; then
    log_info "  Authentication: ACR Admin Credentials"
  else
    log_info "  Authentication: Service Principal"
  fi

  log_info "Deploying container app (this may take 5-10 minutes)..."
  log_info "  Building and pushing container image..."
  log_info "  Creating/updating container app..."
  
  # Check GitHub authentication before deployment
  log_step "Checking GitHub authentication for repository access..."
  
  # Check if GitHub token is available as environment variable
  if [[ -n "${GITHUB_TOKEN:-}" ]]; then
    log_info "GitHub token found in environment"
    export GITHUB_TOKEN
  elif gh auth status &>/dev/null 2>&1; then
    log_info "GitHub CLI is authenticated"
    # Try to get token from gh CLI
    if command -v gh &> /dev/null; then
      local gh_token
      gh_token=$(gh auth token 2>/dev/null || echo "")
      if [[ -n "$gh_token" ]]; then
        export GITHUB_TOKEN="$gh_token"
        log_info "Using GitHub token from gh CLI"
      fi
    fi
  else
    log_warning "GitHub authentication not found."
    log_info "az containerapp up will use device flow authentication."
    log_info "When prompted:"
    log_info "  1. A URL and code will be displayed"
    log_info "  2. Visit the URL in your browser"
    log_info "  3. Enter the code shown in the terminal"
    log_info "  4. Authorize the application"
    log_info ""
    log_info "To avoid this, authenticate GitHub CLI first:"
    log_info "  gh auth login"
    log_info ""
    log_info "Or set GITHUB_TOKEN environment variable"
    log_info "Proceeding with deployment (will prompt if needed)..."
  fi
  
  # Build the command arguments
  # Note: az containerapp up does not support --output flag
  local up_args=(
    --name "$container_app_name"
    --resource-group "$PROJECT_RESOURCE_GROUP"
    --repo "$repo_url"
    --branch "$branch"
    --registry-server "$registry_url"
    --ingress external
    --target-port "$target_port"
  )
  
  # Only add --environment if we found it, otherwise let az containerapp up handle it
  if [[ -n "$environment_name" ]]; then
    up_args+=(--environment "$environment_name")
    log_info "Using Container Apps Environment: $environment_name"
  else
    log_info "az containerapp up will create or select the appropriate environment"
  fi
  
  # Add authentication credentials
  if [[ "$use_acr_admin" == true ]]; then
    up_args+=(
      --registry-username "$acr_username"
      --registry-password "$acr_password"
    )
  else
    up_args+=(
      --service-principal-client-id "$client_id"
      --service-principal-client-secret "$client_secret"
      --service-principal-tenant-id "$tenant_id"
    )
  fi
  
  # Suppress verbose output and only show important messages
  local deploy_output
  local deploy_exit_code=0
  
  # Run deployment and capture output
  # Note: az containerapp up may prompt for GitHub authentication interactively
  # We'll capture both stdout and stderr, and handle interactive prompts
  log_info "Starting deployment (this may prompt for GitHub authentication)..."
  
  # Run deployment
  # Note: az containerapp up may require GitHub authentication via device flow
  # We need to show output in real-time so GitHub auth codes are visible
  log_info "Executing az containerapp up (this may take several minutes)..."
  log_info ""
  log_info "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
  log_info "If GitHub authentication is required, you will see a URL and code below."
  log_info "Visit the URL and enter the code to authorize."
  log_info "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
  log_info ""
  
  # Create a temporary file to capture output while still showing it
  local temp_log
  temp_log=$(mktemp)
  
  # Run deployment with unbuffered output so GitHub auth codes appear immediately
  # We use tee to both show output and capture it for error analysis
  if stdbuf -oL -eL az containerapp up "${up_args[@]}" 2>&1 | stdbuf -oL -eL tee "$temp_log"; then
    deploy_exit_code=0
  else
    deploy_exit_code=${PIPESTATUS[0]}
  fi
  
  log_info ""
  log_info "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
  
  # Read captured output for error analysis
  deploy_output=$(cat "$temp_log" 2>/dev/null || echo "")
  rm -f "$temp_log"
  
  # Check for GitHub authentication prompts in output
  if echo "$deploy_output" | grep -iE "visit.*github|enter.*code|device.*flow|authentication.*required|github.com/login/device" > /dev/null; then
    log_warning "GitHub authentication was required during deployment."
    log_info "If authentication failed, please:"
    log_info "  1. Run: gh auth login"
    log_info "  2. Or set GITHUB_TOKEN environment variable"
    log_info "  3. Then rerun this deployment script"
  fi
  
  # Filter out verbose INFO/WARNING messages from Azure CLI, but keep errors
  # First, let's check if there are any obvious errors before filtering
  if echo "$deploy_output" | grep -iE "error|failed|exception|denied|unauthorized|forbidden|invalid|not found|cannot|unable" > /dev/null; then
    log_error "Deployment error detected. Showing error details:"
    echo "$deploy_output" | grep -iE "error|failed|exception|denied|unauthorized|forbidden|invalid|not found|cannot|unable" | head -10 | sed 's/^/  /'
  fi
  
  local filtered_output
  filtered_output=$(echo "$deploy_output" | \
    grep -v "^INFO:" | \
    grep -v "^WARNING:" | \
    grep -v "Request URL:" | \
    grep -v "Request method:" | \
    grep -v "Request headers:" | \
    grep -v "Response status:" | \
    grep -v "Response headers:" | \
    grep -v "Response content:" | \
    grep -v "^Running" | \
    grep -v "^\\| Running" | \
    grep -v "^/ Running" | \
    grep -v "^\\\\ Running" | \
    grep -v "^\\- Running" | \
    grep -v "^Creating Containerapp" | \
    sed '/^$/d' || true)
  
  if [[ $deploy_exit_code -eq 0 ]]; then
    # Check for errors in output even if exit code is 0
    if echo "$filtered_output" | grep -iE "error|failed|exception" > /dev/null; then
      log_warning "Deployment completed but warnings/errors detected:"
      echo "$filtered_output" | grep -iE "error|failed|exception" | head -5 | sed 's/^/  /'
    fi
    log_success "Container app deployment completed successfully"
  else
    # Show error messages - be more lenient with filtering to catch actual errors
    local error_lines
    error_lines=$(echo "$filtered_output" | grep -iE "error|failed|exception|denied|unauthorized|forbidden|invalid|not found|cannot|unable" || true)
    
    if [[ -n "$error_lines" ]]; then
      log_error "Container app deployment failed:"
      echo "$error_lines" | head -10 | sed 's/^/  /'
    else
      # If no clear error found, show the last meaningful lines
      log_error "Container app deployment failed (exit code: $deploy_exit_code)"
      log_info "Last output lines:"
      echo "$filtered_output" | tail -10 | sed 's/^/  /'
      
      # Also check the raw output for any error patterns we might have missed
      local raw_errors
      raw_errors=$(echo "$deploy_output" | grep -iE "error|failed" | head -5 || true)
      if [[ -n "$raw_errors" ]]; then
        log_info "Additional error details:"
        echo "$raw_errors" | sed 's/^/  /'
      fi
    fi
    return 1
  fi

  log_step "Enabling managed identity for Container App..."
  if az containerapp identity assign \
    --name "$container_app_name" \
    --resource-group "$PROJECT_RESOURCE_GROUP" \
    --system-assigned \
    --output none 2>/dev/null; then
    log_success "Managed identity enabled"
  else
    log_warning "Failed to enable managed identity (may already be enabled)"
  fi

  log_step "Configuring Container App environment variables and resources..."
  
  # Find the actual Key Vault name (may differ from expected pattern)
  local actual_keyvault_name="$keyvault_name"
  if ! az keyvault show --name "$keyvault_name" --resource-group "$PROJECT_RESOURCE_GROUP" --output none &>/dev/null; then
    log_info "Key Vault '$keyvault_name' not found. Searching for Key Vaults matching pattern..."
    
    # Try to find Key Vault with similar name pattern (e.g., dev-bos-kv, dev-bs-kv)
    local found_kv
    found_kv=$(az keyvault list \
      --resource-group "$PROJECT_RESOURCE_GROUP" \
      --query "[?contains(name, '${ENVIRONMENT_PREFIX}') && contains(name, 'kv')].name" -o tsv 2>/dev/null | head -1)
    
    if [[ -n "$found_kv" ]]; then
      actual_keyvault_name="$found_kv"
      log_info "Found existing Key Vault: $actual_keyvault_name (using instead of $keyvault_name)"
    else
      log_warning "No Key Vault found. Container app will be configured without KeyVaultName."
      log_warning "The application may fail to start if it requires Key Vault secrets."
      actual_keyvault_name=""
    fi
  else
    log_info "Found Key Vault: $actual_keyvault_name"
  fi
  
  local aspnetcore_env="Production"
  if [[ "$ENVIRONMENT_PREFIX" == "dev" ]]; then
    aspnetcore_env="Development"
  elif [[ "$ENVIRONMENT_PREFIX" == "staging" ]]; then
    aspnetcore_env="Staging"
  fi
  
  # Build environment variables - only include KeyVaultName if we found one
  if [[ -n "$actual_keyvault_name" ]]; then
    if az containerapp update \
      --name "$container_app_name" \
      --resource-group "$PROJECT_RESOURCE_GROUP" \
      --cpu 0.25 \
      --memory 0.5Gi \
      --min-replicas 1 \
      --max-replicas 10 \
      --set-env-vars "KeyVaultName=$actual_keyvault_name" \
                      "ASPNETCORE_ENVIRONMENT=$aspnetcore_env" \
                      "ASPNETCORE_HTTP_PORTS=$target_port" \
                      "ASPNETCORE_URLS=http://+:$target_port" \
      --output none 2>/dev/null; then
      log_success "Container app configuration updated successfully"
      log_info "  Environment: $aspnetcore_env"
      log_info "  HTTP Port: $target_port"
      log_info "  URLs: http://+:$target_port"
      log_info "  Key Vault: $actual_keyvault_name"
    else
      log_warning "Failed to update container app configuration (may already be configured)"
    fi
  else
    # Update without KeyVaultName
    if az containerapp update \
      --name "$container_app_name" \
      --resource-group "$PROJECT_RESOURCE_GROUP" \
      --cpu 0.25 \
      --memory 0.5Gi \
      --min-replicas 1 \
      --max-replicas 10 \
      --set-env-vars "ASPNETCORE_ENVIRONMENT=$aspnetcore_env" \
                      "ASPNETCORE_HTTP_PORTS=$target_port" \
                      "ASPNETCORE_URLS=http://+:$target_port" \
      --output none 2>/dev/null; then
      log_success "Container app configuration updated successfully"
      log_info "  Environment: $aspnetcore_env"
      log_info "  HTTP Port: $target_port"
      log_info "  URLs: http://+:$target_port"
      log_warning "  Key Vault: Not configured (no Key Vault found)"
    else
      log_warning "Failed to update container app configuration (may already be configured)"
    fi
  fi

  log_step "Granting Container App access to Key Vault..."
  
  # Use the actual Key Vault name we found
  if [[ -z "$actual_keyvault_name" ]]; then
    log_warning "No Key Vault found. Skipping access grant."
    log_info "Run './scripts/setup-keyvault.sh' to create a Key Vault and upload secrets."
    return 0
  fi
  
  # Get Container App's managed identity principal ID
  local principal_id
  principal_id=$(az containerapp show \
    --name "$container_app_name" \
    --resource-group "$PROJECT_RESOURCE_GROUP" \
    --query "identity.principalId" -o tsv 2>/dev/null)
  
  if [[ -z "$principal_id" ]]; then
    log_warning "Could not retrieve principal ID for Container App managed identity."
    log_warning "Managed identity may not be enabled yet. This will be retried on next deployment."
    return 0
  fi
  
  log_info "Container App Principal ID: $principal_id"
  log_info "Granting 'get' and 'list' permissions on secrets..."
  
  # Grant access to Key Vault
  if az keyvault set-policy \
    --name "$actual_keyvault_name" \
    --object-id "$principal_id" \
    --secret-permissions get list \
    --output none 2>/dev/null; then
    log_success "Key Vault access granted to Container App"
    log_info "Container App '$container_app_name' can now read secrets from Key Vault: $actual_keyvault_name"
  else
    log_error "Failed to grant Key Vault access. This may be due to insufficient permissions."
    log_info "You may need to grant access manually using one of these methods:"
    log_info ""
    log_info "Option 1: Using Azure CLI:"
    log_info "  az keyvault set-policy \\"
    log_info "    --name $actual_keyvault_name \\"
    log_info "    --object-id $principal_id \\"
    log_info "    --secret-permissions get list"
    log_info ""
    log_info "Option 2: Using Azure Portal:"
    log_info "  1. Go to Key Vault: $actual_keyvault_name"
    log_info "  2. Navigate to 'Access policies' or 'Access control (IAM)'"
    log_info "  3. Add the Container App's managed identity with 'Get' and 'List' permissions on secrets"
    return 1
  fi
  
  # Get and display the app URL
  local app_url
  app_url=$(az containerapp show \
    --name "$container_app_name" \
    --resource-group "$PROJECT_RESOURCE_GROUP" \
    --query "properties.configuration.ingress.fqdn" -o tsv 2>/dev/null)
  
  if [[ -n "$app_url" ]]; then
    log_success "Container App deployed successfully!"
    log_info "Application URL: https://$app_url"
  fi
}

# ----- Main -----
main() {
  log_info "=========================================="
  log_info "Azure Container Apps Deployment Script"
  log_info "=========================================="
  
  initialize_configuration
  validate_configuration

  local timestamp
  timestamp=$(date +"%Y%m%d_%H%M%S")
  local log_file="${LOG_FOLDER}/deploy_worker_${timestamp}.log"
  mkdir -p "$(dirname "$log_file")"
  exec > >(tee -a "$log_file") 2>&1

  log_info "Starting Container App Deployment Workflow"
  log_info "Log file: $log_file"
  
  setup_azure_context || {
    log_error "Failed to setup Azure context"
    exit 1
  }
  
  ensure_cli_prereqs || {
    log_error "Failed to ensure CLI prerequisites"
    exit 1
  }
  
  ensure_resource_group || {
    log_error "Failed to ensure resource group"
    exit 1
  }

  # Provision ACR + Managed Environment via Bicep (idempotent)
  provision_infra_bicep || {
    log_error "Failed to provision infrastructure"
    exit 1
  }

  prepare_container_registry || {
    log_error "Failed to prepare container registry"
    exit 1
  }
  
  prepare_container_apps_environment || {
    log_error "Failed to prepare container apps environment"
    exit 1
  }
  
  deploy_container_app || {
    log_error "Failed to deploy container app"
    exit 1
  }

  log_info "=========================================="
  log_success "Deployment completed successfully!"
  log_info "=========================================="
  log_info "Detailed logs available at: $log_file"
  log_info "To view logs: tail -f $log_file"
}

main "$@"