using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingApi.DTOs.ShoppingLists;
using ShoppingApi.Services.ShoppingLists;

namespace ShoppingApi.Controllers;

[ApiController]
[Route("api/lists/{listId:int}/shares")]
[Authorize]
public class ListSharesController : ControllerBase
{
    private readonly IShoppingListService _service;

    public ListSharesController(IShoppingListService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ListShareDto>>> GetAll(int listId)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _service.GetSharesAsync(listId, userId, IsAdmin()));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost]
    public async Task<ActionResult<ListShareDto>> Create(int listId, [FromBody] ShareListRequest request)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _service.CreateShareAsync(listId, request, userId, IsAdmin());
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
        catch (DbUpdateException ex) when (
            ex.InnerException is SqlException sqlException &&
            (sqlException.Number == 2601 || sqlException.Number == 2627))
        {
            return Conflict(new { message = "This user already has access to the list." });
        }
    }

    [HttpDelete("{sharedUserId}")]
    public async Task<IActionResult> Delete(int listId, string sharedUserId)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        try
        {
            await _service.RemoveShareAsync(listId, sharedUserId, userId, IsAdmin());
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
    }

    private bool TryGetUserId(out string userId)
    {
        userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        return !string.IsNullOrWhiteSpace(userId);
    }

    private bool IsAdmin() => User.IsInRole("Admin");
}
