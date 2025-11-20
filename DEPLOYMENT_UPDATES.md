# Deployment Configuration Updates

## Summary
All deployment files have been updated to use values from `globalenv.config` and ensure proper port configuration for Azure Container Apps.

## Changes Made

### 1. `scripts/deploy-azure-container-apps.sh`

#### Updated Functions:

- **`prepare_container_registry()`**
  - Now uses `$ACR_NAME` from config instead of hardcoded value
  - Added `--location` parameter for ACR creation

- **`prepare_container_apps_environment()`**
  - Uses `$ACR_NAME` for registry URL construction
  - Removed redundant `-dev` suffix from environment name

- **`deploy_container_app()`**
  - Uses `$ACR_NAME` from config
  - Added support for optional environment variables:
    - `REPOSITORY_URL` (defaults to hk-biashara-os-dev)
    - `GIT_BRANCH` (defaults to 'dev')
    - `TARGET_PORT` (defaults to 8080)
    - Service Principal credentials (optional)
  - **Port Configuration Fix:**
    - Sets `ASPNETCORE_HTTP_PORTS=8080`
    - Sets `ASPNETCORE_URLS=http://+:8080`
    - Ensures the app listens on port 8080 for Azure Container Apps
  - Improved error handling for Key Vault access
  - Better logging for deployment steps

- **`provision_infra_bicep()`**
  - Uses `$REPOSITORY_URL` and `$GIT_BRANCH` from config (with defaults)
  - Ensures `$PROJECT_LOCATION` is always passed to Bicep

- **`validate_configuration()`**
  - Added validation for `ACR_NAME`
  - Added configuration summary logging

### 2. `bicep/parameters.dev.json`

- Updated CPU from `0.5` to `0.25` (matches deployment script)
- Updated Memory from `1.0Gi` to `0.5Gi` (matches deployment script)
- Updated `scaleMax` from `3` to `10` (matches deployment script)
- All other values remain aligned with `globalenv.config`

### 3. `bicep/main.bicep`

- **Port Configuration:**
  - Added `transport: 'auto'` to ingress configuration
  - Target port remains `8080` (correct for Azure Container Apps)
  
- **Environment Variables:**
  - Added `ASPNETCORE_ENVIRONMENT=Production`
  - Added `ASPNETCORE_HTTP_PORTS=8080`
  - Added `ASPNETCORE_URLS=http://+:8080`
  - Ensures the .NET app listens on the correct port

### 4. `globalenv.config`

- Added comprehensive documentation comments
- Documented all required variables
- Documented optional variables with defaults
- Added examples for service principal credentials

## Port Configuration Fix

### Problem
The container app was showing port errors in Azure dashboard because:
1. The app wasn't explicitly configured to listen on port 8080
2. Environment variables weren't set correctly

### Solution
1. **In Bicep Template:**
   - Added environment variables to container definition
   - Set `ASPNETCORE_HTTP_PORTS=8080`
   - Set `ASPNETCORE_URLS=http://+:8080`

2. **In Deployment Script:**
   - Sets environment variables during `az containerapp update`
   - Ensures port 8080 is used consistently

3. **In Application:**
   - .NET apps respect `ASPNETCORE_HTTP_PORTS` and `ASPNETCORE_URLS`
   - Port 8080 is the standard for Azure Container Apps

## Usage

### Basic Deployment
```bash
# Ensure globalenv.config is configured
source globalenv.config

# Run deployment
./scripts/deploy-azure-container-apps.sh
```

### With Custom Repository/Branch
```bash
export REPOSITORY_URL="https://github.com/your-org/your-repo"
export GIT_BRANCH="main"
./scripts/deploy-azure-container-apps.sh
```

### With Service Principal (for private registries)
```bash
export SERVICE_PRINCIPAL_CLIENT_ID="your-client-id"
export SERVICE_PRINCIPAL_CLIENT_SECRET="your-secret"
export SERVICE_PRINCIPAL_TENANT_ID="your-tenant-id"
./scripts/deploy-azure-container-apps.sh
```

## Verification

After deployment, verify the port configuration:

1. **Check Container App Environment Variables:**
   ```bash
   az containerapp show \
     --name "$ENVIRONMENT_PREFIX-$PROJECT_PREFIX-worker" \
     --resource-group "$PROJECT_RESOURCE_GROUP" \
     --query "properties.template.containers[0].env"
   ```

2. **Check Ingress Configuration:**
   ```bash
   az containerapp show \
     --name "$ENVIRONMENT_PREFIX-$PROJECT_PREFIX-worker" \
     --resource-group "$PROJECT_RESOURCE_GROUP" \
     --query "properties.configuration.ingress"
   ```

3. **Test the Application:**
   ```bash
   # Get the app URL
   APP_URL=$(az containerapp show \
     --name "$ENVIRONMENT_PREFIX-$PROJECT_PREFIX-worker" \
     --resource-group "$PROJECT_RESOURCE_GROUP" \
     --query "properties.configuration.ingress.fqdn" -o tsv)
   
   # Test health endpoint
   curl "https://$APP_URL/api/health"
   ```

## Notes

- Port 8080 is the standard for Azure Container Apps
- The app will automatically listen on port 8080 when `ASPNETCORE_HTTP_PORTS=8080` is set
- All resource names are now derived from `globalenv.config` for consistency
- The deployment script is idempotent - safe to run multiple times

