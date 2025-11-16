using ApiWorker.Documents.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiWorker.Documents.Configurations;

/// <summary>
/// Single Table Inheritance configuration for TransactionalDocument.
/// All transactional documents (Invoice, Receipt, Quotation) stored in one table.
/// </summary>
public sealed class TransactionalDocumentConfiguration : IEntityTypeConfiguration<TransactionalDocument>
{
    public void Configure(EntityTypeBuilder<TransactionalDocument> b)
    {
        b.HasBaseType<Document>();

        b.Property(x => x.CustomerName).HasMaxLength(128);
        b.Property(x => x.CustomerPhone).HasMaxLength(32);
        b.Property(x => x.CustomerEmail).HasMaxLength(128);
        b.Property(x => x.BillingAddressLine1).HasMaxLength(256);
        b.Property(x => x.BillingAddressLine2).HasMaxLength(256);
        b.Property(x => x.BillingCity).HasMaxLength(64);
        b.Property(x => x.BillingCountry).HasMaxLength(64);
        b.Property(x => x.Reference).HasMaxLength(64);
        b.Property(x => x.Notes).HasMaxLength(1024);
        b.Property(x => x.DiscountRate).HasColumnType("decimal(5,4)");
        b.Property(x => x.DiscountAmount).HasColumnType("decimal(18,2)");

        b.HasMany(x => x.Lines)
            .WithOne(l => l.Document)
            .HasForeignKey(l => l.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
