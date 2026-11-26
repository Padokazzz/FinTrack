using FinTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Infrastructure.Data;

public class FinTrackDbContext : DbContext
{
    public FinTrackDbContext(DbContextOptions<FinTrackDbContext> options)
        : base(options)
    {
    }

    public DbSet<RefreshSession> RefreshSessions => Set<RefreshSession>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Transaction> Transactions => Set<Transaction>();

    public DbSet<Transfer> Transfers => Set<Transfer>();

    public DbSet<RecurringRule> RecurringRules => Set<RecurringRule>();

    public DbSet<RecurringOccurrence> RecurringOccurrences => Set<RecurringOccurrence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FinTrackDbContext).Assembly);
    }
}
