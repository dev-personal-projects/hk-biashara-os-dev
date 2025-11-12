using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders; // For EntityTypeBuilder


namespace ApiWorker.Authentication.Entities.Configurations;

public sealed class AppUserConfig : Microsoft.EntityFrameworkCore.IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> b)
    {
        b.ToTable("users");

        b.Property(x => x.SupabaseUserId).IsRequired().HasMaxLength(64);
        b.Property(x => x.FullName).IsRequired().HasMaxLength(128);
        b.Property(x => x.Email).IsRequired().HasMaxLength(256);
        b.Property(x => x.County).IsRequired().HasMaxLength(64);

        b.HasIndex(x => x.SupabaseUserId).IsUnique();
        b.HasIndex(x => x.Email).IsUnique();

        b.Property(x => x.RowVersion).IsRowVersion();
    }
}