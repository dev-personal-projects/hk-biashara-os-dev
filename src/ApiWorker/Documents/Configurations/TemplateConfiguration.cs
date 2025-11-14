// src/ApiWorker/Documents/Configurations/TemplateConfiguration.cs
using ApiWorker.Documents.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiWorker.Documents.Configurations;

public sealed class TemplateConfiguration : IEntityTypeConfiguration<Template>
{
    public void Configure(EntityTypeBuilder<Template> b)
    {
        b.ToTable("DocumentTemplates");
        b.HasKey(x => x.Id);

        b.Property(x => x.Type).IsRequired();
        b.Property(x => x.Name).HasMaxLength(128).IsRequired();
        b.Property(x => x.BlobPath).HasMaxLength(256).IsRequired();
        b.Property(x => x.FieldsJson).HasColumnType("nvarchar(max)");

        // Timestamps
        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.UpdatedAt).IsRequired();

        // A business can have multiple versions; ensure uniqueness across name+version
        b.HasIndex(x => new { x.BusinessId, x.Type, x.Name, x.Version }).IsUnique();

        // Often used query
        b.HasIndex(x => new { x.BusinessId, x.Type, x.IsDefault });
    }
}
