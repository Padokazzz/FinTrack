using FinTrack.Domain.Enums;

namespace FinTrack.Application.DTOs.Reports;

public record CategoryTotal(Guid CategoryId, string CategoryName, TransactionType Type, decimal Total, int Count);
