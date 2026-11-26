using FinTrack.Application.Interfaces.Services;
using FinTrack.Infrastructure.Insights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/insights")]
public class InsightsController(SpendingInsights insights, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) =>
        Ok(await insights.GetAsync(currentUser.UserId, ct));

    [HttpPut("limits")]
    public async Task<IActionResult> PutLimits(SpendingLimits limits, CancellationToken ct)
    {
        if (!await insights.SaveAsync(currentUser.UserId, limits, ct))
            return BadRequest(new { message = "Invalid spending limits or expense categories." });

        return NoContent();
    }
}
