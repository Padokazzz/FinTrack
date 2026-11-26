using FinTrack.Api.Models;
using FinTrack.Application.DTOs.Transfers;
using FinTrack.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class TransfersController : ControllerBase
{
    private readonly ITransferService _transferService;
    private readonly ICurrentUserService _currentUserService;

    public TransfersController(
        ITransferService transferService,
        ICurrentUserService currentUserService)
    {
        _transferService = transferService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _transferService.GetAllAsync(
            _currentUserService.UserId,
            cancellationToken);

        return Ok(result.Value);
    }

    [HttpGet("page")]
    public async Task<IActionResult> GetPage(
        [FromQuery] PaginationQuery pagination,
        CancellationToken cancellationToken)
    {
        var response = await _transferService.GetPageAsync(
            _currentUserService.UserId, pagination.Page, pagination.PageSize, cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _transferService.GetByIdAsync(
            id,
            _currentUserService.UserId,
            cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(new { message = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateTransferRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _transferService.CreateAsync(
            _currentUserService.UserId,
            request,
            cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { message = result.Error });
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value.Id },
            result.Value);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _transferService.DeleteAsync(
            id,
            _currentUserService.UserId,
            cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(new { message = result.Error });
        }

        return NoContent();
    }
}
