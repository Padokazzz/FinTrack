using FinTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinTrack.Infrastructure.Data.Configurations;

public class TransferConfiguration : IEntityTypeConfiguration<Transfer>
{
    public void Configure(EntityTypeBuilder<Transfer> builder)
    {
        builder.ToTable("Transfers");

        builder.HasKey(transfer => transfer.Id);

        builder.Property(transfer => transfer.Description)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(transfer => transfer.Amount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(transfer => transfer.Date)
            .IsRequired();

        builder.Property(transfer => transfer.CreatedAt)
            .IsRequired();

        builder.Property(transfer => transfer.UpdatedAt);

        builder.HasOne(transfer => transfer.User)
            .WithMany(user => user.Transfers)
            .HasForeignKey(transfer => transfer.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(transfer => transfer.FromAccount)
            .WithMany(account => account.OutgoingTransfers)
            .HasForeignKey(transfer => transfer.FromAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(transfer => transfer.ToAccount)
            .WithMany(account => account.IncomingTransfers)
            .HasForeignKey(transfer => transfer.ToAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(transfer => new
        {
            transfer.UserId,
            transfer.Date
        });
    }
}
