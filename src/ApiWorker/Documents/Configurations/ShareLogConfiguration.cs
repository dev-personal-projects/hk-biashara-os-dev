// src/ApiWorker/Documents/Configurations/ShareLogConfiguration.cs
using ApiWorker.Documents.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiWorker.Documents.Configurations;

public sealed class ShareLogConfiguration : IEntityTypeConfiguration<ShareLog>
{
    public void Configure(EntityTypeBuilder<ShareLog> b)
    {
        b.ToTable("ShareLogs");
        b.HasKey(x => x.Id);

        b.Property(x => x.Target).HasMaxLength(128).IsRequired();
        b.Property(x => x.MessageId).HasMaxLength(128);
        b.Property(x => x.Error).HasMaxLength(512);

        b.HasIndex(x => new { x.DocumentId, x.SentAt });

        b.HasOne(x => x.Document)
            .WithMany(d => d.ShareLogs)
            .HasForeignKey(x => x.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
