using Microsoft.EntityFrameworkCore;
using ApiWorker.Data;

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

// API services
builder.Services.AddOpenApi();
builder.Services.AddControllers();

var app = builder.Build();

// Development tools
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Middleware pipeline
app.UseHttpsRedirection();
app.MapControllers();

app.Run();
