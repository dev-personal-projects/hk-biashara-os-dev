// src/ApiWorker/Documents/Configurations/InvoiceLineConfiguration.cs
using ApiWorker.Documents.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiWorker.Documents.Configurations;

public sealed class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> b)
    {
        b.ToTable("InvoiceLines");
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).HasMaxLength(128).IsRequired();
        b.Property(x => x.Description).HasMaxLength(512);

        b.Property(x => x.Quantity).HasColumnType("decimal(18,3)");
        b.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
        b.Property(x => x.TaxRate).HasColumnType("decimal(5,4)");
        b.Property(x => x.LineTotal).HasColumnType("decimal(18,2)");

        b.HasIndex(x => x.InvoiceId);
    }
}
