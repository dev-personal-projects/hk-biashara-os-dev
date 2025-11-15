// src/ApiWorker/Documents/Configurations/InvoiceConfiguration.cs
using ApiWorker.Documents.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiWorker.Documents.Configurations;

/// <summary>
/// TPT mapping: Invoice extends Document (PK = FK).
/// </summary>
public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> b)
    {
        b.ToTable("Invoices");
        b.HasBaseType<Document>();

        // Customer/billing sizes for SQL
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

        // Lines
        b.HasMany(x => x.Lines)
            .WithOne(l => l.Invoice)
            .HasForeignKey(l => l.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
