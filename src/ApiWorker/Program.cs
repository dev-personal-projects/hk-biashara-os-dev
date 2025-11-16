using Microsoft.EntityFrameworkCore;
using ApiWorker.Data;
using ApiWorker.Authentication.Extensions;
using ApiWorker.Authentication.Middleware;
using ApiWorker.Speech.Extensions;
using ApiWorker.Speech.Interfaces;
using ApiWorker.Speech.Services;
using ApiWorker.Speech.Storage;
using ApiWorker.Cosmos.Extensions;
using ApiWorker.Documents.Extensions;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Extensions.AspNetCore.Configuration.Secrets;

var builder = WebApplication.CreateBuilder(args);

// ===== AZURE KEY VAULT CONFIGURATION =====
// ALWAYS load secrets from Azure Key Vault using Managed Identity
// No fallback to local credentials - Key Vault is the single source of truth
// Get Key Vault name from environment variable (set by Container App)
var keyVaultName = builder.Configuration["KeyVaultName"] 
    ?? throw new InvalidOperationException("KeyVaultName environment variable not set. Cannot load secrets.");

var keyVaultUri = new Uri($"https://{keyVaultName}.vault.azure.net/");

// DefaultAzureCredential: Uses Managed Identity in Azure (Container App)
// For local development, use Azure CLI: az login
var credential = new DefaultAzureCredential();

// SecretClient: Azure SDK client for Key Vault operations
var secretClient = new SecretClient(keyVaultUri, credential);

// Add Key Vault as configuration source
// Secret naming convention: "ConnectionStrings--Default" maps to ConnectionStrings:Default
// All secrets from Key Vault override any values in appsettings.json
builder.Configuration.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());

// ===== DATABASE CONFIGURATION =====
// Entity Framework Core with Azure SQL Database
// Connection string MUST be loaded from Key Vault
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' not found in Key Vault.");

// Register DbContext with SQL Server provider
// DbContext lifetime: Scoped (new instance per HTTP request)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sql =>
    {
        // Automatically retry transient failures (network issues, timeouts)
        sql.EnableRetryOnFailure();
        // Specify assembly for migrations (required for class library projects)
        sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
    }));

// ===== MODULE REGISTRATION =====
// Each module has its own extension method for clean separation of concerns

/// <summary>
/// Authentication & Authorization Module
/// - JWT token generation and validation
/// - User and business management
/// - Supabase OAuth integration
/// </summary>
builder.Services.AddAuthenticationInfrastructure(builder.Configuration);

/// <summary>
/// Cosmos DB Module (NoSQL storage)
/// - High-volume data storage (transcripts, analytics)
/// - Partition-based tenant isolation
/// - Global distribution support
/// </summary>
builder.Services.AddCosmosInfrastructure(builder.Configuration);

/// <summary>
/// Speech Module (Voice-to-Text)
/// - Azure Speech SDK integration
/// - Multi-locale support (en-KE, sw-KE)
/// - Transcription storage in Cosmos DB
/// </summary>
builder.Services.AddSpeechInfrastructure(builder.Configuration);

/// <summary>
/// Documents Module
/// - Invoice, receipt, quotation generation
/// - Template management
/// - Document sharing (WhatsApp, PDF)
/// </summary>
builder.Services.AddDocumentsInfrastructure(builder.Configuration);

// ===== SPEECH ORCHESTRATION SERVICES =====
// These services combine multiple modules (Speech + Cosmos)

/// <summary>
/// ITranscriptionStore: Repository for storing transcripts in Cosmos DB.
/// Scoped lifetime: New instance per HTTP request (safe with async operations).
/// </summary>
builder.Services.AddScoped<ITranscriptionStore, CosmosTranscriptionStore>();

/// <summary>
/// ISpeechCaptureService: High-level orchestrator for STT + storage.
/// Scoped lifetime: New instance per HTTP request.
/// </summary>
builder.Services.AddScoped<ISpeechCaptureService, SpeechCaptureService>();

// ===== AZURE BLOB STORAGE =====
// Used for storing documents, templates, and audio files
// Connection string MUST be loaded from Key Vault
var blobConnectionString = builder.Configuration.GetConnectionString("BlobStorage")
    ?? throw new InvalidOperationException("Connection string 'BlobStorage' not found in Key Vault.");

/// <summary>
/// BlobServiceClient: Azure Blob Storage client.
/// Singleton lifetime: Thread-safe, reuses connections across requests.
/// </summary>
builder.Services.AddSingleton(x => new Azure.Storage.Blobs.BlobServiceClient(blobConnectionString));

/// <summary>
/// IBlobStorageService: Abstraction over Azure Blob Storage.
/// Scoped lifetime: New instance per HTTP request.
/// </summary>
builder.Services.AddScoped<ApiWorker.Storage.IBlobStorageService, ApiWorker.Storage.BlobStorageService>();

// ===== API INFRASTRUCTURE =====

/// <summary>
/// OpenAPI/Swagger: API documentation and testing UI.
/// Only enabled in development (see app.MapOpenApi() below).
/// </summary>
builder.Services.AddOpenApi();

/// <summary>
/// Controllers: MVC controllers for REST API endpoints.
/// Automatically discovers all controllers in the assembly.
/// </summary>
builder.Services.AddControllers();

/// <summary>
/// CORS: Cross-Origin Resource Sharing for web clients.
/// WARNING: AllowAnyOrigin is insecure for production!
/// TODO: Restrict to specific origins in production.
/// </summary>
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()   // TODO: Change to .WithOrigins("https://app.biasharaos.com")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Development tools
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
}

// ===== MIDDLEWARE PIPELINE =====
// Order matters! Each middleware processes requests in sequence.

/// <summary>
/// HTTPS Redirection: Redirects HTTP requests to HTTPS.
/// Should be first to ensure all traffic is encrypted.
/// </summary>
app.UseHttpsRedirection();

/// <summary>
/// CORS: Handles cross-origin requests from web clients.
/// Must be before authentication to allow preflight requests.
/// </summary>
app.UseCors();

/// <summary>
/// Authentication: Validates JWT tokens and sets User principal.
/// Must be before authorization and custom user middleware.
/// </summary>
app.UseAuthentication();

/// <summary>
/// CurrentUserMiddleware: Loads user and business context from JWT.
/// Populates HttpContext.Items with User, Business, etc.
/// Must be after authentication but before authorization.
/// </summary>
app.UseMiddleware<CurrentUserMiddleware>();

/// <summary>
/// Authorization: Enforces [Authorize] attributes on controllers.
/// Must be after authentication and user context middleware.
/// </summary>
app.UseAuthorization();

/// <summary>
/// Map Controllers: Routes HTTP requests to controller actions.
/// Should be last in the pipeline.
/// </summary>
app.MapControllers();

app.Run();