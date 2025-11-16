using ApiWorker.Documents.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiWorker.Documents.Configurations;

public sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> b)
    {
        b.ToTable("Documents");
        b.HasKey(x => x.Id);

        // Single Table Inheritance discriminator
        b.HasDiscriminator<DocumentType>("Type")
            .HasValue<Invoice>(DocumentType.Invoice)
            .HasValue<TransactionalDocument>(DocumentType.Invoice);

        // Base properties
        b.Property(x => x.Type).IsRequired();
        b.Property(x => x.Status).IsRequired();

        b.Property(x => x.Number)
            .HasMaxLength(32)
            .IsRequired();

        b.Property(x => x.Currency)
            .HasMaxLength(3)
            .IsRequired();

        // Money precision for Azure SQL
        b.Property(x => x.Subtotal).HasColumnType("decimal(18,2)");
        b.Property(x => x.Tax).HasColumnType("decimal(18,2)");
        b.Property(x => x.Total).HasColumnType("decimal(18,2)");

        // Storage
        b.Property(x => x.DocxBlobUrl).HasMaxLength(512);
        b.Property(x => x.PdfBlobUrl).HasMaxLength(512);
        b.Property(x => x.CosmosId).HasMaxLength(128);

        // Ownership
        b.HasIndex(x => x.BusinessId);
        b.HasIndex(x => x.CreatedByUserId);

        // Unique: (Business, Type, Number)
        b.HasIndex(x => new { x.BusinessId, x.Type, x.Number }).IsUnique();

        // Timestamps
        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.UpdatedAt).IsRequired();

        // Optimistic concurrency
        b.Property(x => x.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        // Minimal guard: total >= 0
        // (Check constraints keep DB sane even if service bugs slip in)
        b.ToTable(t => t.HasCheckConstraint("CK_Documents_TotalNonNegative", "[Total] >= 0"));
    }
}
