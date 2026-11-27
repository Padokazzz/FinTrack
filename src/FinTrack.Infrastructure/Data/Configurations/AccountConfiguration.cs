using FinTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinTrack.Infrastructure.Data.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");

        builder.HasKey(account => account.Id);

        builder.Property<uint>("xmin").IsRowVersion();

        builder.Property(account => account.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(account => account.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(account => account.InitialBalance)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(account => account.CurrentBalance)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(account => account.CreatedAt)
            .IsRequired();

        builder.Property(account => account.UpdatedAt);

        builder.Property(account => account.CreditLimit).HasPrecision(18, 2);

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(account => account.LinkedBankAccountId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(account => account.User)
            .WithMany(user => user.Accounts)
            .HasForeignKey(account => account.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}