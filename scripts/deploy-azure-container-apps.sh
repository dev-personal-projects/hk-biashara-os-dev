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
log_error()   { echo -e "${RED}[ERROR] $*${NC}" >&2; }
log_info()    { echo -e "${BLUE}[INFO] $*${NC}"; }
log_success() { echo -e "${GREEN}[SUCCESS] $*${NC}"; }
log_warning() { echo -e "${YELLOW}[WARNING] $*${NC}"; }

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
    az group create -n "$PROJECT_RESOURCE_GROUP" -l "$PROJECT_LOCATION" >/dev/null
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
}

# ----- Provision Infra with Bicep (idempotent) -----
provision_infra_bicep() {
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

  log_info "Deploying infra via Bicep (template: $template_file)"
  if [[ -f "$params_file" ]]; then
    log_info "Using parameters file: $params_file"
    az deployment group create \
      --resource-group "$PROJECT_RESOURCE_GROUP" \
      --template-file "$template_file" \
      --parameters @"$params_file"
  else
    log_warning "Parameters file not found at ${params_file}. Using inline parameters."
    az deployment group create \
      --resource-group "$PROJECT_RESOURCE_GROUP" \
      --template-file "$template_file" \
      --parameters environment="$ENVIRONMENT_PREFIX" \
                   repositoryUrl="https://github.com/dev-personal-projects/hk-biashara-os-dev" \
                   branch="dev" \
                   location="$PROJECT_LOCATION"
  fi
}

# ----- Prepare ACR -----
prepare_container_registry() {
  local registry_name="${ENVIRONMENT_PREFIX}${PROJECT_PREFIX}contregistry"
  log_info "Checking Azure Container Registry: $registry_name"

  if ! az acr show --name "$registry_name" &>/dev/null; then
    log_warning "Container Registry does not exist. Creating..."
    az acr create \
      --name "$registry_name" \
      --resource-group "$PROJECT_RESOURCE_GROUP" \
      --sku Basic \
      --admin-enabled true
  fi

  az acr login --name "$registry_name"
}

# ----- Prepare Container Apps Environment -----
prepare_container_apps_environment() {
  local environment_name="${ENVIRONMENT_PREFIX}-${PROJECT_PREFIX}-BackendContainerAppsEnv-dev"
  local container_app_name="${ENVIRONMENT_PREFIX}-${PROJECT_PREFIX}-worker"
  local registry_url="${ENVIRONMENT_PREFIX}${PROJECT_PREFIX}contregistry.azurecr.io"

  log_info "Preparing Container Apps Environment: $environment_name"

  if ! az containerapp env show --name "$environment_name" --resource-group "$PROJECT_RESOURCE_GROUP" &>/dev/null; then
    log_warning "Container Apps Environment does not exist. Creating..."
    az containerapp env create \
      --name "$environment_name" \
      --resource-group "$PROJECT_RESOURCE_GROUP" \
      --location "$PROJECT_LOCATION"
  fi

  echo "Environment Name: $environment_name"
  echo "Container App Name: $container_app_name"
  echo "Registry URL: $registry_url"
}

# ----- Build & Deploy Container App -----
deploy_container_app() {
  local environment_name="${ENVIRONMENT_PREFIX}-${PROJECT_PREFIX}-BackendContainerAppsEnv"
  local container_app_name="${ENVIRONMENT_PREFIX}-${PROJECT_PREFIX}-worker"
  local registry_url="${ENVIRONMENT_PREFIX}${PROJECT_PREFIX}contregistry.azurecr.io"
  local repo_url="https://github.com/dev-personal-projects/hk-biashara-os-dev"
  local branch="dev"
  local client_id="b744d9b5-68b1-4e3e-82d6-93698d50a3fb"
  local client_secret="wP18Q~FVBlhquwxUh4X7EP.nbmXcbZsLGpRyfbzV"
  local tenant_id="55d15577-df36-46ee-9782-f7b38ae4ea3c"
  local keyvault_name="${ENVIRONMENT_PREFIX}-${PROJECT_PREFIX}-kv"

  log_info "Deploying Container App: $container_app_name"

  az containerapp up \
    --name "$container_app_name" \
    --resource-group "$PROJECT_RESOURCE_GROUP" \
    --environment "$environment_name" \
    --repo "$repo_url" \
    --branch "$branch" \
    --registry-server "$registry_url" \
    --service-principal-client-id "$client_id" \
    --service-principal-client-secret "$client_secret" \
    --service-principal-tenant-id "$tenant_id" \
    --ingress external \
    --target-port 8000

  log_info "Enabling managed identity for Container App"
  az containerapp identity assign \
    --name "$container_app_name" \
    --resource-group "$PROJECT_RESOURCE_GROUP" \
    --system-assigned

  log_info "Configuring Container App scaling and resources"
  az containerapp update \
    --name "$container_app_name" \
    --resource-group "$PROJECT_RESOURCE_GROUP" \
    --cpu 0.25 \
    --memory 0.5Gi \
    --min-replicas 1 \
    --max-replicas 10 \
    --set-env-vars "KeyVaultName=$keyvault_name" "ASPNETCORE_ENVIRONMENT=Production"

  log_info "Granting Container App access to Key Vault"
  local principal_id
  principal_id=$(az containerapp show \
    --name "$container_app_name" \
    --resource-group "$PROJECT_RESOURCE_GROUP" \
    --query "identity.principalId" -o tsv)

  az keyvault set-policy \
    --name "$keyvault_name" \
    --object-id "$principal_id" \
    --secret-permissions get list
}

# ----- Main -----
main() {
  initialize_configuration
  validate_configuration

  local timestamp
  timestamp=$(date +"%Y%m%d_%H%M%S")
  local log_file="${LOG_FOLDER}/deploy_worker_${timestamp}.log"
  exec > >(tee -a "$log_file") 2>&1

  log_info "Starting Container App Deployment Workflow"
  setup_azure_context
  ensure_cli_prereqs
  ensure_resource_group

  # NEW: Provision ACR + Managed Environment via Bicep (idempotent)
  provision_infra_bicep

  # Your original flow continues unchanged
  prepare_container_registry
  prepare_container_apps_environment
  deploy_container_app

  log_success "Deployment completed successfully"
  log_info "Detailed logs available at: $log_file"
}

main "$@"