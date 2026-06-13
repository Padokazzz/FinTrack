using FinTrack.Application.DTOs.Categories;
using FinTrack.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly ICurrentUserService _currentUserService;

    public CategoriesController(
        ICategoryService categoryService,
        ICurrentUserService currentUserService)
    {
        _categoryService = categoryService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _categoryService.GetAllAsync(
            _currentUserService.UserId,
            cancellationToken);

        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _categoryService.GetByIdAsync(
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
        CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _categoryService.CreateAsync(
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
        UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _categoryService.UpdateAsync(
            id,
            _currentUserService.UserId,
            request,
            cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(new { message = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _categoryService.DeleteAsync(
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