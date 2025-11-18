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
using ApiWorker.Configuration.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddKeyVaultConfiguration();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Default") 
            ?? throw new InvalidOperationException("Database connection string not found in Key Vault."),
        sql =>
        {
            sql.EnableRetryOnFailure();
            sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
        }));

builder.Services.AddAuthenticationInfrastructure(builder.Configuration);
builder.Services.AddCosmosInfrastructure(builder.Configuration);
builder.Services.AddSpeechInfrastructure(builder.Configuration);
builder.Services.AddDocumentsInfrastructure(builder.Configuration);
builder.Services.AddBlobStorage(builder.Configuration);

builder.Services.AddScoped<ITranscriptionStore, CosmosTranscriptionStore>();
builder.Services.AddScoped<ISpeechCaptureService, SpeechCaptureService>();

builder.Services.AddOpenApi();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Allow string enum values in JSON (e.g., "Invoice" instead of 1)
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseMiddleware<CurrentUserMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.Run();