using FinTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinTrack.Infrastructure.Data.Configurations;

public class RecurringRuleConfiguration : IEntityTypeConfiguration<RecurringRule>
{
    public void Configure(EntityTypeBuilder<RecurringRule> builder)
    {
        builder.Property<uint>("xmin").IsRowVersion();
        builder.Property(rule => rule.Description).HasMaxLength(150);
        builder.Property(rule => rule.Amount).HasPrecision(18, 2);
        builder.HasIndex(rule => new { rule.Paused, rule.NextDate });
        builder.HasIndex(rule => rule.UserId);
        builder.HasOne<User>().WithMany().HasForeignKey(rule => rule.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Account>().WithMany().HasForeignKey(rule => rule.AccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Category>().WithMany().HasForeignKey(rule => rule.CategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class RecurringOccurrenceConfiguration : IEntityTypeConfiguration<RecurringOccurrence>
{
    public void Configure(EntityTypeBuilder<RecurringOccurrence> builder)
    {
        builder.HasKey(occurrence => new { occurrence.RuleId, occurrence.Date });
        builder.Property<uint>("xmin").IsRowVersion();
        builder.Property(occurrence => occurrence.OriginalAmount).HasPrecision(18, 2);
        builder.Property(occurrence => occurrence.OriginalDescription).HasMaxLength(150);
        builder.HasOne<Account>().WithMany().HasForeignKey(occurrence => occurrence.OriginalAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Category>().WithMany().HasForeignKey(occurrence => occurrence.OriginalCategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(occurrence => occurrence.Rule).WithMany().HasForeignKey(occurrence => occurrence.RuleId);
    }
}

public class RecurrenceAmortizationConfiguration : IEntityTypeConfiguration<RecurrenceAmortization>
{
    public void Configure(EntityTypeBuilder<RecurrenceAmortization> builder)
    {
        builder.Property(item => item.Amount).HasPrecision(18, 2);
        builder.Property(item => item.RemainingBefore).HasPrecision(18, 2);
        builder.Property(item => item.RemainingAfter).HasPrecision(18, 2);
        builder.Property(item => item.ComparedPayment).HasPrecision(18, 2);
        builder.Property(item => item.Savings).HasPrecision(19, 2);
        builder.HasOne(item => item.Rule).WithMany(rule => rule.Amortizations).HasForeignKey(item => item.RuleId);
    }
}
