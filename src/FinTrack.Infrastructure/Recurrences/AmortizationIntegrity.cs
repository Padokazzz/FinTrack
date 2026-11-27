using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using FinTrack.Domain.Entities;

namespace FinTrack.Infrastructure.Recurrences;

public sealed class AmortizationIntegrity(byte[] key)
{
    public byte[] Sign(RecurringRule rule)
    {
        // This format is versioned so future formats can retain verification of older signatures.
        using var payload = new MemoryStream();
        using var writer = new BinaryWriter(payload, Encoding.UTF8, leaveOpen: true);
        writer.Write("fintrack:amortizations:v1");
        writer.Write(rule.UserId.ToByteArray());
        writer.Write(rule.Id.ToByteArray());
        writer.Write(rule.Amortizations.Count);
        foreach (var item in rule.Amortizations.OrderBy(item => item.Id))
        {
            writer.Write(item.Id.ToByteArray());
            writer.Write(item.Date.DayNumber);
            writer.Write(item.Installments);
            WriteMoney(writer, item.Amount);
            WriteMoney(writer, item.RemainingBefore);
            WriteMoney(writer, item.RemainingAfter);
            WriteMoney(writer, item.ComparedPayment);
            WriteMoney(writer, item.Savings);
        }
        writer.Flush();
        return HMACSHA256.HashData(key, payload.GetBuffer().AsSpan(0, (int)payload.Length));
    }

    public void Verify(RecurringRule rule)
    {
        if (rule.AmortizationSignature is null ||
            !CryptographicOperations.FixedTimeEquals(rule.AmortizationSignature, Sign(rule)))
            throw new AmortizationIntegrityException(rule.Id);
    }

    private static void WriteMoney(BinaryWriter writer, decimal? value)
    {
        writer.Write(value.HasValue);
        if (value.HasValue)
            writer.Write(value.Value.ToString("F2", CultureInfo.InvariantCulture));
    }
}

public sealed class AmortizationIntegrityException(Guid ruleId) : Exception("Amortization history failed integrity verification.")
{
    public Guid RuleId { get; } = ruleId;
}

public sealed class AmortizationHistoryExistsException() : Exception("Pause the schedule to retain its amortization history.");
