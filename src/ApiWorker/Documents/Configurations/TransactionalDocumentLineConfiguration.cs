using ApiWorker.Documents.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiWorker.Documents.Configurations;

public sealed class TransactionalDocumentLineConfiguration : IEntityTypeConfiguration<TransactionalDocumentLine>
{
    public void Configure(EntityTypeBuilder<TransactionalDocumentLine> b)
    {
        b.ToTable("TransactionalDocumentLines");
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).HasMaxLength(128).IsRequired();
        b.Property(x => x.Description).HasMaxLength(512);
        b.Property(x => x.Quantity).HasColumnType("decimal(18,3)");
        b.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
        b.Property(x => x.TaxRate).HasColumnType("decimal(5,4)");
        b.Property(x => x.LineTotal).HasColumnType("decimal(18,2)");

        b.HasIndex(x => x.DocumentId);
    }
}
