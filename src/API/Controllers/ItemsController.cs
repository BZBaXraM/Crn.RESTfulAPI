using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Asp.Versioning;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/products/{productId:int}/items")]
[Produces("application/json")]
public sealed class ItemsController : ControllerBase
{
    private readonly IItemService _items;

    public ItemsController(IItemService items)
    {
        _items = items;
    }

    /// <summary>Returns a paginated list of items belonging to a product.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<ItemDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<ItemDto>>> GetAll(int productId, [FromQuery] PaginationQuery query, CancellationToken cancellationToken)
        => Ok(await _items.GetByProductAsync(productId, query, cancellationToken));

    /// <summary>Returns a single item belonging to a product.</summary>
    [HttpGet("{itemId:int}", Name = nameof(GetItemById))]
    [ProducesResponseType<ItemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ItemDto>> GetItemById(int productId, int itemId, CancellationToken cancellationToken)
        => Ok(await _items.GetByIdAsync(productId, itemId, cancellationToken));

    /// <summary>Adds an item to a product.</summary>
    [HttpPost]
    [Authorize(Roles = nameof(Role.Admin))]
    [ProducesResponseType<ItemDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ItemDto>> Create(int productId, [FromBody] CreateItemRequest request, CancellationToken cancellationToken)
    {
        var created = await _items.CreateAsync(productId, request, cancellationToken);
        return CreatedAtRoute(nameof(GetItemById), new { version = "1.0", productId, itemId = created.Id }, created);
    }

    /// <summary>Updates an item belonging to a product.</summary>
    [HttpPut("{itemId:int}")]
    [Authorize(Roles = nameof(Role.Admin))]
    [ProducesResponseType<ItemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ItemDto>> Update(int productId, int itemId, [FromBody] UpdateItemRequest request, CancellationToken cancellationToken)
        => Ok(await _items.UpdateAsync(productId, itemId, request, cancellationToken));

    /// <summary>Removes an item from a product.</summary>
    [HttpDelete("{itemId:int}")]
    [Authorize(Roles = nameof(Role.Admin))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int productId, int itemId, CancellationToken cancellationToken)
    {
        await _items.DeleteAsync(productId, itemId, cancellationToken);
        return NoContent();
    }
}
