using Microsoft.EntityFrameworkCore;
using ApiWorker.Data;
using ApiWorker.Authentication.Extensions;
using ApiWorker.Authentication.Middleware;

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