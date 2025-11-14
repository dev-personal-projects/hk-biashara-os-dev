using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiWorker.Authentication.Entities.Configurations;

public sealed class BusinessConfig : IEntityTypeConfiguration<Business>
{
    public void Configure(EntityTypeBuilder<Business> b)
    {
        b.ToTable("businesses");

        b.Property(x => x.Name).IsRequired().HasMaxLength(128);
        b.Property(x => x.Category).IsRequired().HasMaxLength(64);
        b.Property(x => x.County).IsRequired().HasMaxLength(64);
        b.Property(x => x.Town).HasMaxLength(96);
        b.Property(x => x.Email).HasMaxLength(256);
        b.Property(x => x.Phone).HasMaxLength(32);

        b.Property(x => x.Latitude).HasColumnType("decimal(9,6)");
        b.Property(x => x.Longitude).HasColumnType("decimal(9,6)");

        b.HasIndex(x => x.Name);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasOne(x => x.DefaultTemplate)
         .WithMany()
         .HasForeignKey(x => x.DefaultTemplateId)
         .OnDelete(DeleteBehavior.NoAction);
    }
}
