using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ApiWorker.Data;

// Allows `dotnet ef ...` to create the DbContext at design-time.
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // env var takes precedence, then appsettings.Development.json
        var builder = new ConfigurationBuilder()
            .AddEnvironmentVariables() // supports ConnectionStrings__Default
            .AddJsonFile("appsettings.Development.json", optional: true);

        var config = builder.Build();
        var cs = config.GetConnectionString("Default")
                 ?? throw new InvalidOperationException("ConnectionStrings:Default not set.");

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(cs, sql =>
            {
                sql.EnableRetryOnFailure();
                sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
            })
            .Options;

        return new ApplicationDbContext(options);
    }
}
