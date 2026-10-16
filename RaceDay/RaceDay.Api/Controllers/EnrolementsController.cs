using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    public class EnrolmentsController : ControllerBase
    {
        private readonly RaceDayDbContext _db;

        public EnrolmentsController(RaceDayDbContext db)
        {
            _db = db;
        }

        /// <summary>Enrols the logged-in Participant in a category.</summary>
        /// <response code="201">Enrolment created.</response>
        /// <response code="400">Validation failed or the event has already taken place.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Only Participants may enrol.</response>
        /// <response code="404">Category not found.</response>
        /// <response code="409">Already enrolled in this category.</response>
        [HttpPost("api/enrolments")]
        [SessionAuthorize("Participant")]
        [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<EnrolmentDto>> Enrol(CreateEnrolmentDto dto)
        {
            var userId = HttpContext.Session.GetInt32("UserId")!.Value;

            var category = await _db.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryId == dto.CategoryId);

            if (category == null) return NotFound("Category not found.");

            if (category.Event.EventDate < DateTime.UtcNow)
                return BadRequest("This event has already taken place.");

            var alreadyEnrolled = await _db.Enrolments
                .AnyAsync(e => e.ParticipantId == userId && e.CategoryId == dto.CategoryId);

            if (alreadyEnrolled)
                return Conflict("You are already enrolled in this category.");

            var enrolment = new Enrolment
            {
                ParticipantId = userId,
                EventId = category.EventId,
                CategoryId = category.CategoryId,
                EnrolmentDate = DateTime.UtcNow,
                Status = "Confirmed"
            };

            _db.Enrolments.Add(enrolment);
            await _db.SaveChangesAsync();

            var result = new EnrolmentDto
            {
                EnrolmentId = enrolment.EnrolmentId,
                EventId = category.EventId,
                EventName = category.Event.Name,
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                EnrolmentDate = enrolment.EnrolmentDate
            };

            return CreatedAtAction(nameof(GetMine), null, result);
        }

        /// <summary>Lists the logged-in Participant's own enrolments.</summary>
        /// <response code="200">List returned.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Only Participants have enrolments.</response>
        [HttpGet("api/enrolments/mine")]
        [SessionAuthorize("Participant")]
        [ProducesResponseType(typeof(List<EnrolmentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<List<EnrolmentDto>>> GetMine()
        {
            var userId = HttpContext.Session.GetInt32("UserId")!.Value;

            var list = await _db.Enrolments
                .AsNoTracking()
                .Where(e => e.ParticipantId == userId)
                .OrderByDescending(e => e.EnrolmentDate)
                .Select(e => new EnrolmentDto
                {
                    EnrolmentId = e.EnrolmentId,
                    EventId = e.EventId,
                    EventName = e.Event.Name,
                    CategoryId = e.CategoryId,
                    CategoryName = e.Category.CategoryName,
                    EnrolmentDate = e.EnrolmentDate
                })
                .ToListAsync();

            return Ok(list);
        }

        /// <summary>Lists everyone enrolled in an event. Only the owning Organiser may do this.</summary>
        /// <response code="200">List returned.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Not an Organiser, or not the owner of this event.</response>
        /// <response code="404">Event not found.</response>
        [HttpGet("api/events/{eventId}/enrolments")]
        [SessionAuthorize("Organiser")]
        [ProducesResponseType(typeof(List<EventEnrolmentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<EventEnrolmentDto>>> GetForEvent(int eventId)
        {
            var userId = HttpContext.Session.GetInt32("UserId")!.Value;

            var ev = await _db.Events.AsNoTracking()
                .FirstOrDefaultAsync(e => e.EventId == eventId);

            if (ev == null) return NotFound();
            if (ev.OrganiserId != userId) return Forbid();

            var list = await _db.Enrolments
                .AsNoTracking()
                .Where(e => e.EventId == eventId)
                .OrderBy(e => e.Category.CategoryName)
                .Select(e => new EventEnrolmentDto
                {
                    EnrolmentId = e.EnrolmentId,
                    UserId = e.ParticipantId,
                    ParticipantName = e.Participant.FullName,
                    ParticipantEmail = e.Participant.Email,
                    CategoryId = e.CategoryId,
                    CategoryName = e.Category.CategoryName,
                    EnrolmentDate = e.EnrolmentDate
                })
                .ToListAsync();

            return Ok(list);
        }
    }
}