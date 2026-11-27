using System.Security.Cryptography;
using FinTrack.Domain.Entities;
using FinTrack.Infrastructure.Recurrences;

namespace FinTrack.Tests.Recurrences;

public class AmortizationIntegrityTests
{
    [Fact]
    public void VerificationRejectsChangesAndRemovalFromSignedHistory()
    {
        var integrity = new AmortizationIntegrity(RandomNumberGenerator.GetBytes(32));
        var rule = new RecurringRule { UserId = Guid.NewGuid() };
        var entry = new RecurrenceAmortization
        {
            Id = Guid.NewGuid(), Date = new DateOnly(2026, 11, 27), Amount = 12m
        };
        rule.Amortizations.Add(entry);
        rule.AmortizationSignature = integrity.Sign(rule);

        entry.Amount = 12.00m;
        integrity.Verify(rule);
        entry.Savings = 1m;
        Assert.Throws<AmortizationIntegrityException>(() => integrity.Verify(rule));
        entry.Savings = null;
        rule.Amortizations.Remove(entry);
        Assert.Throws<AmortizationIntegrityException>(() => integrity.Verify(rule));
        rule.Amortizations.Add(entry);
        integrity.Verify(rule);
    }
}
