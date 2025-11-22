# Secret Management for Key Vault Setup

The `setup-keyvault.sh` script uploads secrets to Azure Key Vault. **All secrets must be provided via environment variables** - never hardcode secrets in scripts or commit them to version control.

## Required Environment Variables

These secrets are **required** and must be set before running the script:

```bash
# SQL Database Connection String
export DB_CONNECTION_STRING="Server=tcp:your-server.database.windows.net,1433;Initial Catalog=your-db;Persist Security Info=False;User ID=your-user;Password=your-password;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

# Azure Blob Storage Connection String
export BLOB_STORAGE_CONNECTION_STRING="DefaultEndpointsProtocol=https;AccountName=your-account;AccountKey=your-key;EndpointSuffix=core.windows.net"

# JWT Secret Key (base64 encoded, minimum 32 bytes)
export JWT_SECRET_KEY="your-base64-encoded-secret-key"
```

## Optional Environment Variables

These secrets are **optional** and will only be uploaded if provided:

```bash
# Cosmos DB
export COSMOS_ENDPOINT="https://your-account.documents.azure.com:443/"
export COSMOS_KEY="your-cosmos-db-key"

# Supabase Authentication
export SUPABASE_URL="https://your-project.supabase.co"
export SUPABASE_KEY="your-supabase-anon-key"

# Azure Speech Services
export SPEECH_KEY="your-azure-speech-key"
export VOICE_KEY="your-azure-voice-key"

# Azure OpenAI
export AZURE_OPENAI_API_KEY="your-openai-api-key"

# WhatsApp Integration
export WHATSAPP_ACCESS_TOKEN="your-meta-whatsapp-token"
```

## Usage

### Option 1: Export Environment Variables

```bash
export DB_CONNECTION_STRING="..."
export BLOB_STORAGE_CONNECTION_STRING="..."
export JWT_SECRET_KEY="..."
./scripts/setup-keyvault.sh
```

### Option 2: Use a .env File (Recommended)

Create a `.env` file in the project root (add it to `.gitignore`):

```bash
# .env (DO NOT COMMIT THIS FILE)
DB_CONNECTION_STRING="Server=tcp:..."
BLOB_STORAGE_CONNECTION_STRING="DefaultEndpointsProtocol=https;..."
JWT_SECRET_KEY="your-secret-key"
```

Then source it before running the script:

```bash
set -a  # Automatically export all variables
source .env
set +a
./scripts/setup-keyvault.sh
```

### Option 3: Use Azure Key Vault to Store Secrets Temporarily

If you need to retrieve secrets from another Key Vault:

```bash
# Retrieve from another Key Vault
export DB_CONNECTION_STRING=$(az keyvault secret show --vault-name "source-kv" --name "DBConnectionString" --query value -o tsv)
export BLOB_STORAGE_CONNECTION_STRING=$(az keyvault secret show --vault-name "source-kv" --name "BlobStorageConnectionString" --query value -o tsv)
export JWT_SECRET_KEY=$(az keyvault secret show --vault-name "source-kv" --name "JwtSecretKey" --query value -o tsv)

./scripts/setup-keyvault.sh
```

## Security Best Practices

1. **Never commit secrets to version control**
   - Add `.env` to `.gitignore`
   - Never hardcode secrets in scripts
   - Use environment variables or secure secret stores

2. **Use Azure Key Vault for production**
   - Store all secrets in Azure Key Vault
   - Use Managed Identity for applications
   - Rotate secrets regularly

3. **Limit access to secrets**
   - Only grant access to necessary services
   - Use Azure RBAC for Key Vault access
   - Monitor Key Vault access logs

4. **Generate strong secrets**
   - Use cryptographically secure random generators
   - Minimum 32 bytes for JWT secrets
   - Rotate secrets periodically

## Generating Secrets

### Generate a JWT Secret Key

```bash
# Using OpenSSL (base64 encoded, 32 bytes)
export JWT_SECRET_KEY=$(openssl rand -base64 32)

# Using Python
export JWT_SECRET_KEY=$(python3 -c "import secrets; print(secrets.token_urlsafe(32))")
```

### Generate Database Password

```bash
# Strong password (32 characters)
export DB_PASSWORD=$(openssl rand -base64 24 | tr -d "=+/" | cut -c1-32)
```

## Troubleshooting

### Error: "Missing required environment variables"

Make sure all required environment variables are set:

```bash
echo $DB_CONNECTION_STRING
echo $BLOB_STORAGE_CONNECTION_STRING
echo $JWT_SECRET_KEY
```

### Error: "Failed to upload secret"

- Verify you have permissions to write to the Key Vault
- Check that the Key Vault exists
- Ensure you're authenticated with Azure CLI: `az account show`

### Secret not appearing in Key Vault

- Check the secret name matches exactly (case-sensitive)
- Verify the upload succeeded (check script output)
- List secrets: `az keyvault secret list --vault-name "your-kv-name"`

