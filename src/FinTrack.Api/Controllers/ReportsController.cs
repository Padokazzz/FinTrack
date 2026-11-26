using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using FinTrack.Application.DTOs.Transactions;
using FinTrack.Application.Interfaces.Repositories;
using FinTrack.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ReportsController(
    ITransactionRepository repository,
    ITransactionService transactions,
    ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories([FromQuery] TransactionFilterRequest filter, CancellationToken cancellationToken)
    {
        var totals = await repository.GetCategoryTotalsAsync(currentUser.UserId, filter, cancellationToken);
        return Ok(totals);
    }

    [HttpGet("transactions.csv")]
    public async Task<IActionResult> Export(
        [FromQuery, Range(1, 12)] int month,
        [FromQuery, Range(2000, 2100)] int year,
        CancellationToken cancellationToken)
    {
        var result = await transactions.GetFilteredAsync(
            currentUser.UserId, new TransactionFilterRequest { Month = month, Year = year }, cancellationToken);
        var csv = new StringBuilder("date,description,account,category,type,amount\r\n");

        foreach (var transaction in result.Value)
        {
            csv.Append(transaction.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append(',')
                .Append(CsvCell(transaction.Description)).Append(',')
                .Append(CsvCell(transaction.AccountName)).Append(',')
                .Append(CsvCell(transaction.CategoryName)).Append(',')
                .Append(transaction.Type).Append(',')
                .Append(transaction.Amount.ToString("0.00", CultureInfo.InvariantCulture)).Append("\r\n");
        }

        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(),
            "text/csv; charset=utf-8", $"fintrack-{year:D4}-{month:D2}.csv");
    }

    private static string CsvCell(string value)
    {
        var trimmed = value.TrimStart();
        if (trimmed.StartsWith('=') || trimmed.StartsWith('+') || trimmed.StartsWith('-') ||
            trimmed.StartsWith('@') || value.StartsWith('\t') || value.StartsWith('\r') || value.StartsWith('\n'))
        {
            value = "'" + value;
        }

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
