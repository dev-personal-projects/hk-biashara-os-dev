using System.Reflection;
using Microsoft.EntityFrameworkCore;
using ApiWorker.Authentication.Entities;
using ApiWorker.Documents.Entities;
using AuthTemplate = ApiWorker.Authentication.Entities.Template;
using DocTemplate = ApiWorker.Documents.Entities.Template;

namespace ApiWorker.Data;

public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // Authentication
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<AuthTemplate> AuthTemplates => Set<AuthTemplate>();
    
    // Documents
    public DbSet<ApiWorker.Documents.Entities.Document> Documents => Set<ApiWorker.Documents.Entities.Document>();
    public DbSet<ApiWorker.Documents.Entities.TransactionalDocument> TransactionalDocuments => Set<ApiWorker.Documents.Entities.TransactionalDocument>();
    public DbSet<ApiWorker.Documents.Entities.Invoice> Invoices => Set<ApiWorker.Documents.Entities.Invoice>();
    public DbSet<ApiWorker.Documents.Entities.Receipt> Receipts => Set<ApiWorker.Documents.Entities.Receipt>();
    public DbSet<ApiWorker.Documents.Entities.Quotation> Quotations => Set<ApiWorker.Documents.Entities.Quotation>();
    public DbSet<ApiWorker.Documents.Entities.TransactionalDocumentLine> TransactionalDocumentLines => Set<ApiWorker.Documents.Entities.TransactionalDocumentLine>();
    public DbSet<DocTemplate> DocumentTemplates => Set<DocTemplate>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration<> in Entities/Configurations
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Foreign key relationships: Document -> Business & AppUser
        modelBuilder.Entity<Document>()
            .HasOne<Business>()
            .WithMany()
            .HasForeignKey(d => d.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Document>()
            .HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(d => d.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Document>()
            .HasOne<DocTemplate>()
            .WithMany()
            .HasForeignKey(d => d.TemplateId)
            .OnDelete(DeleteBehavior.SetNull);

        // Foreign key relationship: DocumentTemplate -> Business
        modelBuilder.Entity<DocTemplate>()
            .HasOne<Business>()
            .WithMany()
            .HasForeignKey(t => t.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        // Optional: enforce one default template per (Business, Type)
        modelBuilder.Entity<DocTemplate>()
            .HasIndex(x => new { x.BusinessId, x.Type })
            .IsUnique()
            .HasFilter("[IsDefault] = 1");
    }
}
