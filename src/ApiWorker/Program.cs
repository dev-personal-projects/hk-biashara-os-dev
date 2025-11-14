using Microsoft.EntityFrameworkCore;
using ApiWorker.Data;
using ApiWorker.Authentication.Extensions;
using ApiWorker.Authentication.Middleware;
using ApiWorker.Speech.Extensions;
using ApiWorker.Speech.Interfaces;
using ApiWorker.Speech.Services;
using ApiWorker.Speech.Storage;
using ApiWorker.Cosmos.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Database configuration
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sql =>
    {
        sql.EnableRetryOnFailure();
        sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
    }));

// Authentication & Authorization
builder.Services.AddAuthenticationInfrastructure(builder.Configuration);

// Cosmos DB (NoSQL storage for transcripts, analytics)
builder.Services.AddCosmosInfrastructure(builder.Configuration);

// Speech Services (Azure Speech SDK for STT)
builder.Services.AddSpeechServices(builder.Configuration);

// Speech Orchestration (STT + Cosmos storage)
builder.Services.AddScoped<ITranscriptionStore, CosmosTranscriptionStore>();
builder.Services.AddScoped<ISpeechCaptureService, SpeechCaptureService>();

// Azure Blob Storage
var blobConnectionString = builder.Configuration.GetConnectionString("BlobStorage")
    ?? throw new InvalidOperationException("Connection string 'BlobStorage' not found.");
builder.Services.AddSingleton(x => new Azure.Storage.Blobs.BlobServiceClient(blobConnectionString));
builder.Services.AddScoped<ApiWorker.Storage.IBlobStorageService, ApiWorker.Storage.BlobStorageService>();

// API services
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
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

// Middleware pipeline (order matters)
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseMiddleware<CurrentUserMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.Run();