using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShoppingApi.DTOs.ProductTypes;
using ShoppingApi.Services.Products;

namespace ShoppingApi.Controllers;

[ApiController]
[Route("api/product-types")]
[Authorize]
public class ProductTypesController : ControllerBase
{
    private readonly IProductCatalogService _service;

    public ProductTypesController(IProductCatalogService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductTypeDto>>> GetAll()
    {
        return Ok(await _service.GetProductTypesAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductTypeDto>> GetById(int id)
    {
        var result = await _service.GetProductTypeAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ProductTypeDto>> Create([FromBody] CreateProductTypeRequest request)
    {
        try
        {
            var userId = User.Identity?.Name ?? "system";
            var result = await _service.CreateProductTypeAsync(request, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (Exception ex) when (ex is ValidationException || ex is InvalidOperationException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductTypeDto>> Update(int id, [FromBody] UpdateProductTypeRequest request)
    {
        try
        {
            var userId = User.Identity?.Name ?? "system";
            var result = await _service.UpdateProductTypeAsync(id, request, userId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex) when (ex is ValidationException || ex is InvalidOperationException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var userId = User.Identity?.Name ?? "system";
            await _service.DeleteProductTypeAsync(id, userId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
