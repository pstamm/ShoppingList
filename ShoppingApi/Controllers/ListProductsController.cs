using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingApi.DTOs.ShoppingLists;
using ShoppingApi.Services.ShoppingLists;

namespace ShoppingApi.Controllers;

[ApiController]
[Route("api/lists/{listId:int}/products")]
[Authorize]
public class ListProductsController : ControllerBase
{
    private readonly IShoppingListService _service;

    public ListProductsController(IShoppingListService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ListProductDto>>> GetAll(int listId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            var result = await _service.GetListProductsAsync(listId, userId, IsAdmin());
            return result is null ? NotFound() : Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost]
    public async Task<ActionResult<ListProductDto>> Create(int listId, [FromBody] AddListProductRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            var result = await _service.AddListProductAsync(listId, request, userId, IsAdmin());
            return CreatedAtAction(nameof(GetAll), new { listId }, result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ListProductDto>> Update(
        int listId,
        int id,
        [FromBody] UpdateListProductRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            var result = await _service.UpdateListProductAsync(listId, id, request, userId, IsAdmin());
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "The list product was changed by another user. Reload it and try again." });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int listId, int id, [FromQuery] string rowVersion)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            await _service.DeleteListProductAsync(listId, id, rowVersion, userId, IsAdmin());
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "The list product was changed by another user. Reload it and try again." });
        }
    }

    private bool IsAdmin() => User.IsInRole("Admin");
}
