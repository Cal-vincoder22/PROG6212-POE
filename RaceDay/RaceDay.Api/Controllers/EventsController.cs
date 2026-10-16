using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EventsController(RaceDayDbContext db)
    {
        _db = db;
    }

    private int CurrentUserId => HttpContext.Session.GetInt32("UserId")!.Value;

    /// <summary>
    /// Lists all events. Optionally filter by event type (Run, Walk, Cycle) or show upcoming events only.
    /// </summary>
    /// <response code="200">List of events, soonest first.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEvents([FromQuery] string? eventType, [FromQuery] bool upcomingOnly = false)
    {
        var query = _db.Events.AsQueryable();

        if (!string.IsNullOrWhiteSpace(eventType))
            query = query.Where(e => e.EventType == eventType);

        if (upcomingOnly)
            query = query.Where(e => e.EventDate >= DateTime.Today);

        var events = await query
            .OrderBy(e => e.EventDate)
            .Select(e => new
            {
                e.EventId,
                e.Name,
                e.Description,
                e.EventDate,
                e.Location,
                e.DistanceKm,
                e.EventType,
                e.BannerImageUrl,
                OrganiserName = e.Organiser.FullName,
                EnrolmentCount = e.Enrolments.Count
            })
            .ToListAsync();

        return Ok(events);
    }

    /// <summary>
    /// Returns full detail for a single event, including its categories.
    /// </summary>
    /// <response code="200">Event returned.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEvent(int id)
    {
        var ev = await _db.Events
            .Where(e => e.EventId == id)
            .Select(e => new
            {
                e.EventId,
                e.OrganiserId,
                e.Name,
                e.Description,
                e.EventDate,
                e.Location,
                e.DistanceKm,
                e.EventType,
                e.BannerImageUrl,
                OrganiserName = e.Organiser.FullName,
                Categories = e.Categories.Select(c => new
                {
                    c.CategoryId,
                    c.CategoryName,
                    c.MinAge,
                    c.MaxAge,
                    c.DistanceKm
                })
            })
            .FirstOrDefaultAsync();

        if (ev == null)
            return NotFound(new { message = "Event not found." });

        return Ok(ev);
    }

    /// <summary>
    /// Creates a new event owned by the logged-in Organiser.
    /// </summary>
    /// <response code="201">Event created.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not an Organiser.</response>
    [HttpPost]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateEvent([FromBody] EventDto dto)
    {
        var ev = new Event
        {
            OrganiserId = CurrentUserId,
            Name = dto.Name.Trim(),
            Description = dto.Description,
            EventDate = dto.EventDate.Date,
            Location = dto.Location.Trim(),
            DistanceKm = dto.DistanceKm,
            EventType = dto.EventType
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEvent), new { id = ev.EventId }, new
        {
            ev.EventId,
            ev.Name,
            ev.EventDate,
            ev.Location,
            ev.DistanceKm,
            ev.EventType
        });
    }

    /// <summary>
    /// Updates an event. Only the Organiser who created it may update it.
    /// </summary>
    /// <response code="200">Event updated.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not the owning Organiser.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpPut("{id}")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvent(int id, [FromBody] EventDto dto)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null)
            return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only modify your own events." });

        ev.Name = dto.Name.Trim();
        ev.Description = dto.Description;
        ev.EventDate = dto.EventDate.Date;
        ev.Location = dto.Location.Trim();
        ev.DistanceKm = dto.DistanceKm;
        ev.EventType = dto.EventType;

        await _db.SaveChangesAsync();
        return Ok(new { ev.EventId, ev.Name, ev.EventDate, ev.Location, ev.DistanceKm, ev.EventType });
    }

    /// <summary>
    /// Deletes an event and its categories. Only the owning Organiser may delete it.
    /// </summary>
    /// <response code="204">Event deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not the owning Organiser.</response>
    /// <response code="404">Event does not exist.</response>
    /// <response code="409">Event has enrolments and cannot be deleted.</response>
    [HttpDelete("{id}")]
    [SessionAuthorize("Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null)
            return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only delete your own events." });

        if (await _db.Enrolments.AnyAsync(en => en.EventId == id))
            return Conflict(new { message = "This event has enrolments and cannot be deleted." });

        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}