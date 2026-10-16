using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    public class ResultsController : ControllerBase
    {
        private readonly RaceDayDbContext _db;

        public ResultsController(RaceDayDbContext db)
        {
            _db = db;
        }

        /// <summary>Captures a result for an enrolment. Only the owning Organiser may do this.</summary>
        /// <response code="201">Result captured.</response>
        /// <response code="400">Validation failed or the enrolment was cancelled.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Not an Organiser, or not the owner of this event.</response>
        /// <response code="404">Enrolment not found.</response>
        /// <response code="409">A result already exists for this enrolment.</response>
        [HttpPost("api/enrolments/{enrolmentId}/result")]
        [SessionAuthorize("Organiser")]
        [ProducesResponseType(typeof(ResultDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ResultDto>> Capture(int enrolmentId, CaptureResultDto dto)
        {
            var userId = HttpContext.Session.GetInt32("UserId")!.Value;

            var enrolment = await _db.Enrolments
                .Include(e => e.Event)
                .Include(e => e.Category)
                .Include(e => e.Participant)
                .FirstOrDefaultAsync(e => e.EnrolmentId == enrolmentId);

            if (enrolment == null) return NotFound("Enrolment not found.");
            if (enrolment.Event.OrganiserId != userId) return Forbid();

            if (enrolment.Status == "Cancelled")
                return BadRequest("Cannot capture a result for a cancelled enrolment.");

            if (dto.FinishTime <= TimeSpan.Zero)
                return BadRequest("Finish time must be greater than zero.");

            var exists = await _db.Results.AnyAsync(r => r.EnrolmentId == enrolmentId);
            if (exists)
                return Conflict("A result has already been captured for this enrolment.");

            var result = new Result
            {
                EnrolmentId = enrolmentId,
                FinishTime = dto.FinishTime,
                FinishPosition = dto.FinishPosition,
                CapturedAt = DateTime.UtcNow
            };

            _db.Results.Add(result);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMine), null, ToDto(result, enrolment));
        }

        /// <summary>Corrects an existing result. Only the owning Organiser may do this.</summary>
        /// <response code="200">Result updated.</response>
        /// <response code="400">Validation failed.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Not an Organiser, or not the owner of this event.</response>
        /// <response code="404">Result not found.</response>
        [HttpPut("api/results/{id}")]
        [SessionAuthorize("Organiser")]
        [ProducesResponseType(typeof(ResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ResultDto>> Update(int id, CaptureResultDto dto)
        {
            var userId = HttpContext.Session.GetInt32("UserId")!.Value;

            var result = await _db.Results
                .Include(r => r.Enrolment).ThenInclude(e => e.Event)
                .Include(r => r.Enrolment).ThenInclude(e => e.Category)
                .Include(r => r.Enrolment).ThenInclude(e => e.Participant)
                .FirstOrDefaultAsync(r => r.ResultId == id);

            if (result == null) return NotFound();
            if (result.Enrolment.Event.OrganiserId != userId) return Forbid();

            if (dto.FinishTime <= TimeSpan.Zero)
                return BadRequest("Finish time must be greater than zero.");

            result.FinishTime = dto.FinishTime;
            result.FinishPosition = dto.FinishPosition;
            result.CapturedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(ToDto(result, result.Enrolment));
        }

        /// <summary>Lists the logged-in Participant's own results.</summary>
        /// <response code="200">List returned.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Only Participants have results.</response>
        [HttpGet("api/results/mine")]
        [SessionAuthorize("Participant")]
        [ProducesResponseType(typeof(List<ResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<List<ResultDto>>> GetMine()
        {
            var userId = HttpContext.Session.GetInt32("UserId")!.Value;

            var list = await _db.Results
                .AsNoTracking()
                .Where(r => r.Enrolment.ParticipantId == userId)
                .OrderByDescending(r => r.CapturedAt)
                .Select(r => new ResultDto
                {
                    ResultId = r.ResultId,
                    EnrolmentId = r.EnrolmentId,
                    EventId = r.Enrolment.EventId,
                    EventName = r.Enrolment.Event.Name,
                    CategoryName = r.Enrolment.Category.CategoryName,
                    ParticipantName = r.Enrolment.Participant.FullName,
                    FinishTime = r.FinishTime,
                    FinishPosition = r.FinishPosition,
                    CapturedAt = r.CapturedAt
                })
                .ToListAsync();

            return Ok(list);
        }

        private static ResultDto ToDto(Result r, Enrolment e) => new ResultDto
        {
            ResultId = r.ResultId,
            EnrolmentId = r.EnrolmentId,
            EventId = e.EventId,
            EventName = e.Event.Name,
            CategoryName = e.Category.CategoryName,
            ParticipantName = e.Participant.FullName,
            FinishTime = r.FinishTime,
            FinishPosition = r.FinishPosition,
            CapturedAt = r.CapturedAt
        };
    }
}