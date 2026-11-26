using FinTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinTrack.Infrastructure.Data.Configurations;

public class RefreshSessionConfiguration : IEntityTypeConfiguration<RefreshSession>
{
    public void Configure(EntityTypeBuilder<RefreshSession> builder)
    {
        builder.Property<uint>("xmin").IsRowVersion();
        builder.Property(session => session.TokenHash).HasMaxLength(64);
        builder.HasIndex(session => session.ExpiresAt);
        builder.HasOne(session => session.User).WithMany()
            .HasForeignKey(session => session.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
