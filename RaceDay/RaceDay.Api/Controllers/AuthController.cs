using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public AuthController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Registers a new user as an Organiser or a Participant. The password is hashed with BCrypt before it is stored.
    /// </summary>
    /// <response code="201">User created. The password is never returned.</response>
    /// <response code="400">Validation failed or the role is not Organiser/Participant.</response>
    /// <response code="409">The email address is already registered.</response>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var roleName = dto.Role.Trim().ToLower();
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName.ToLower() == roleName);
        if (role == null)
            return BadRequest(new { message = "Role must be either 'Organiser' or 'Participant'." });

        var email = dto.Email.Trim().ToLower();
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return Conflict(new { message = "An account with this email already exists." });

        var user = new User
        {
            FullName = dto.FullName.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            RoleId = role.RoleId,
            PhoneNumber = dto.PhoneNumber
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new
        {
            user.UserId,
            user.FullName,
            user.Email,
            Role = role.RoleName
        });
    }

    /// <summary>
    /// Logs a user in and starts a server-side session that stores their user ID and role.
    /// </summary>
    /// <response code="200">Login successful. Session started.</response>
    /// <response code="401">Invalid email or password.</response>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var email = dto.Email.Trim().ToLower();
        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        HttpContext.Session.SetInt32("UserId", user.UserId);
        HttpContext.Session.SetString("Role", user.Role.RoleName);

        return Ok(new
        {
            user.UserId,
            user.FullName,
            Role = user.Role.RoleName
        });
    }

    /// <summary>
    /// Ends the current user's session.
    /// </summary>
    /// <response code="200">Session cleared.</response>
    /// <response code="401">No active session.</response>
    [HttpPost("logout")]
    [SessionAuthorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok(new { message = "Logged out." });
    }
}