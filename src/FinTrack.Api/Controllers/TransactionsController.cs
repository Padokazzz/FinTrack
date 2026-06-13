using FinTrack.Application.DTOs.Transactions;
using FinTrack.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly ICurrentUserService _currentUserService;

    public TransactionsController(
        ITransactionService transactionService,
        ICurrentUserService currentUserService)
    {
        _transactionService = transactionService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> GetFiltered(
        [FromQuery] TransactionFilterRequest filter,
        CancellationToken cancellationToken)
    {
        var result = await _transactionService.GetFilteredAsync(
            _currentUserService.UserId,
            filter,
            cancellationToken);

        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _transactionService.GetByIdAsync(
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
        CreateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _transactionService.CreateAsync(
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

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _transactionService.UpdateAsync(
            id,
            _currentUserService.UserId,
            request,
            cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _transactionService.DeleteAsync(
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