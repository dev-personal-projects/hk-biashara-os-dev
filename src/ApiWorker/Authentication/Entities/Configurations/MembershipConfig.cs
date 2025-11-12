using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiWorker.Authentication.Entities.Configurations;

public sealed class MembershipConfig : IEntityTypeConfiguration<Membership>
{
    public void Configure(EntityTypeBuilder<Membership> b)
    {
        b.ToTable("memberships");

        b.HasIndex(x => new { x.UserId, x.BusinessId }).IsUnique();

        b.HasOne(x => x.User)
         .WithMany(u => u.Memeberships)
         .HasForeignKey(x => x.UserId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Business)
         .WithMany(t => t.Memberships)
         .HasForeignKey(x => x.BusinessId)
         .OnDelete(DeleteBehavior.Cascade);

        b.Property(x => x.RowVersion).IsRowVersion();
    }
}
