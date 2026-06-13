using FinTrack.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class SummariesController : ControllerBase
{
    private readonly ISummaryService _summaryService;
    private readonly ICurrentUserService _currentUserService;

    public SummariesController(
        ISummaryService summaryService,
        ICurrentUserService currentUserService)
    {
        _summaryService = summaryService;
        _currentUserService = currentUserService;
    }

    [HttpGet("monthly")]
    public async Task<IActionResult> GetMonthly(
        [FromQuery] int month,
        [FromQuery] int year,
        CancellationToken cancellationToken)
    {
        var result = await _summaryService.GetMonthlySummaryAsync(
            _currentUserService.UserId,
            month,
            year,
            cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(result.Value);
    }
}