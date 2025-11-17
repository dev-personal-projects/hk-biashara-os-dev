using ApiWorker.Documents.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiWorker.Documents.Configurations;

/// <summary>
/// Single Table Inheritance: Invoice is stored in TransactionalDocuments table.
/// Discriminator column 'Type' = DocumentType.Invoice
/// </summary>
public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> b)
    {
        b.HasBaseType<TransactionalDocument>();
    }
}
