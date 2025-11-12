using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiWorker.Authentication.Entities.Configurations;

public sealed class TemplateConfig : IEntityTypeConfiguration<Template>
{
    public void Configure(EntityTypeBuilder<Template> b)
    {
        b.ToTable("templates");

        b.Property(x => x.Name).IsRequired().HasMaxLength(96);
        b.Property(x => x.JsonDefinition).IsRequired().HasColumnType("nvarchar(max)");

        // One default per business per doc-type.
        b.HasIndex(x => new { x.BusinessId, x.DocType, x.IsDefault });

        // Validate JSON content on SQL Server.
        b.ToTable(t => t.HasCheckConstraint(
            "CK_Templates_JsonDefinition_IsJson",
            "ISJSON([JsonDefinition]) = 1"
        ));

        b.HasOne(x => x.Business)
         .WithMany(t => t.Templates)
         .HasForeignKey(x => x.BusinessId)
         .OnDelete(DeleteBehavior.Cascade);

        b.Property(x => x.RowVersion).IsRowVersion();
    }
}
