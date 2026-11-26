using FinTrack.Application.DTOs.Recurrences;
using FinTrack.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class RecurrencesController(IRecurrenceService service, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await service.ListAsync(currentUser.UserId, cancellationToken));

    [HttpGet("agenda")]
    public async Task<IActionResult> Agenda(CancellationToken cancellationToken) =>
        Ok(await service.GetAgendaAsync(currentUser.UserId, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(CreateRecurrenceRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(currentUser.UserId, request, cancellationToken);
        return result.IsSuccess ? StatusCode(StatusCodes.Status201Created, result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> SetPaused(Guid id, RecurrenceStatusRequest request, CancellationToken cancellationToken) =>
        await service.SetPausedAsync(currentUser.UserId, id, request.Paused, cancellationToken) ? NoContent() : NotFound();

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        await service.DeleteAsync(currentUser.UserId, id, cancellationToken) ? NoContent() : NotFound();

    [HttpPost("{id:guid}/occurrences/{date}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, DateOnly date, ConfirmOccurrenceRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ConfirmAsync(currentUser.UserId, id, date, request.Amount, cancellationToken);
        return result.IsSuccess ? NoContent() : BadRequest(new { message = result.Error });
    }
}
