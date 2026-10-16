using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public CategoriesController(RaceDayDbContext db)
    {
        _db = db;
    }

    private int CurrentUserId => HttpContext.Session.GetInt32("UserId")!.Value;

    /// <summary>
    /// Lists all categories available for a specific event.
    /// </summary>
    /// <response code="200">List of categories.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpGet("api/events/{eventId}/categories")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategories(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
            return NotFound(new { message = "Event not found." });

        var categories = await _db.Categories
            .Where(c => c.EventId == eventId)
            .Select(c => new { c.CategoryId, c.EventId, c.CategoryName, c.MinAge, c.MaxAge, c.DistanceKm })
            .ToListAsync();

        return Ok(categories);
    }

    /// <summary>
    /// Adds an age or distance category to an event. Only the owning Organiser may do this.
    /// </summary>
    /// <response code="201">Category created.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not the owning Organiser.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpPost("api/events/{eventId}/categories")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateCategory(int eventId, [FromBody] CategoryDto dto)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null)
            return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only add categories to your own events." });

        if (dto.MinAge.HasValue && dto.MaxAge.HasValue && dto.MaxAge < dto.MinAge)
            return BadRequest(new { message = "MaxAge cannot be less than MinAge." });

        var category = new Category
        {
            EventId = eventId,
            CategoryName = dto.CategoryName.Trim(),
            MinAge = dto.MinAge,
            MaxAge = dto.MaxAge,
            DistanceKm = dto.DistanceKm
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new
        {
            category.CategoryId,
            category.EventId,
            category.CategoryName,
            category.MinAge,
            category.MaxAge,
            category.DistanceKm
        });
    }

    /// <summary>
    /// Updates a category. Only the Organiser who owns the event may do this.
    /// </summary>
    /// <response code="200">Category updated.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not the owning Organiser.</response>
    /// <response code="404">Category does not exist.</response>
    [HttpPut("api/categories/{id}")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryDto dto)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category == null)
            return NotFound(new { message = "Category not found." });

        if (category.Event.OrganiserId != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only modify categories on your own events." });

        if (dto.MinAge.HasValue && dto.MaxAge.HasValue && dto.MaxAge < dto.MinAge)
            return BadRequest(new { message = "MaxAge cannot be less than MinAge." });

        category.CategoryName = dto.CategoryName.Trim();
        category.MinAge = dto.MinAge;
        category.MaxAge = dto.MaxAge;
        category.DistanceKm = dto.DistanceKm;

        await _db.SaveChangesAsync();
        return Ok(new { category.CategoryId, category.EventId, category.CategoryName, category.MinAge, category.MaxAge, category.DistanceKm });
    }

    /// <summary>
    /// Deletes a category. Only the owning Organiser may do this.
    /// </summary>
    /// <response code="204">Category deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not the owning Organiser.</response>
    /// <response code="404">Category does not exist.</response>
    /// <response code="409">Participants have already enrolled in this category.</response>
    [HttpDelete("api/categories/{id}")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category == null)
            return NotFound(new { message = "Category not found." });

        if (category.Event.OrganiserId != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only delete categories on your own events." });

        if (await _db.Enrolments.AnyAsync(en => en.CategoryId == id))
            return Conflict(new { message = "Participants have enrolled in this category, so it cannot be deleted." });

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}