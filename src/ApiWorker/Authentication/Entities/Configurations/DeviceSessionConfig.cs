using ApiWorker.Authentication.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiWorker.Entities.Configurations;

public sealed class DeviceSessionConfig : IEntityTypeConfiguration<DeviceSession>
{
    public void Configure(EntityTypeBuilder<DeviceSession> b)
    {
        b.ToTable("device_sessions");

        b.Property(x => x.DeviceId).IsRequired().HasMaxLength(128);
        b.Property(x => x.Platform).IsRequired().HasMaxLength(16);

        b.HasIndex(x => new { x.UserId, x.DeviceId }).IsUnique();

        b.HasOne(x => x.User)
         .WithMany()
         .HasForeignKey(x => x.UserId)
         .OnDelete(DeleteBehavior.Cascade);

        b.Property(x => x.RowVersion).IsRowVersion();
    }
}
