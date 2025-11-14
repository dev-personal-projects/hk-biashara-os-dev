using System.Reflection;
using Microsoft.EntityFrameworkCore;
using ApiWorker.Authentication.Entities;

namespace ApiWorker.Data;

// EF Core DbContext for Azure SQL. Config is added in Program.cs.
public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<Template> Templates => Set<Template>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration<> in Entities/Configurations
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Optional: enforce one default template per (Business, DocType)
        modelBuilder.Entity<Template>()
            .HasIndex(x => new { x.BusinessId, x.DocType })
            .IsUnique()
            .HasFilter("[IsDefault] = 1");
    }
}
