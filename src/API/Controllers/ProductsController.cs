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
[Route("api/v{version:apiVersion}/products")]
[Produces("application/json")]
public sealed class ProductsController : ControllerBase
{
    private readonly IProductService _products;

    public ProductsController(IProductService products)
    {
        _products = products;
    }

    /// <summary>Returns a paginated list of products.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<ProductDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetAll([FromQuery] PaginationQuery query, CancellationToken cancellationToken)
        => Ok(await _products.GetPagedAsync(query, cancellationToken));

    /// <summary>Returns a single product by id.</summary>
    [HttpGet("{id:int}", Name = nameof(GetProductById))]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetProductById(int id, CancellationToken cancellationToken)
        => Ok(await _products.GetByIdAsync(id, cancellationToken));

    /// <summary>Creates a new product.</summary>
    [HttpPost]
    [Authorize(Roles = nameof(Role.Admin))]
    [ProducesResponseType<ProductDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var created = await _products.CreateAsync(request, cancellationToken);
        return CreatedAtRoute(nameof(GetProductById), new { version = "1.0", id = created.Id }, created);
    }

    /// <summary>Updates an existing product.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = nameof(Role.Admin))]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> Update(int id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
        => Ok(await _products.UpdateAsync(id, request, cancellationToken));

    /// <summary>Deletes a product and its items.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = nameof(Role.Admin))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _products.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
