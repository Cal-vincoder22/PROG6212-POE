using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Filters;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    [Route("api/users")]
    [SessionAuthorize]
    public class UsersController : ControllerBase
    {
        private readonly RaceDayDbContext _db;

        public UsersController(RaceDayDbContext db)
        {
            _db = db;
        }

        /// <summary>Returns the profile of the currently logged-in user.</summary>
        /// <response code="200">Profile returned.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="404">User no longer exists.</response>
        [HttpGet("me")]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserProfileDto>> GetMe()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var user = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null) return NotFound();

            return Ok(new UserProfileDto
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Role = HttpContext.Session.GetString("Role") ?? string.Empty,
                ProfilePictureUrl = user.ProfilePictureUrl
            });
        }

        /// <summary>Updates the name and profile picture of the logged-in user.</summary>
        /// <response code="200">Profile updated.</response>
        /// <response code="400">Validation failed.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="404">User no longer exists.</response>
        [HttpPut("me")]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserProfileDto>> UpdateMe(UpdateProfileDto dto)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null) return NotFound();

            user.FullName = dto.FullName;
            user.ProfilePictureUrl = dto.ProfilePictureUrl;
            await _db.SaveChangesAsync();

            return Ok(new UserProfileDto
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Role = HttpContext.Session.GetString("Role") ?? string.Empty,
                ProfilePictureUrl = user.ProfilePictureUrl
            });
        }
    }
}