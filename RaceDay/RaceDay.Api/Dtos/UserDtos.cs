using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos
{
    /// <summary>The profile details returned for the logged-in user.</summary>
    public class UserProfileDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
    }

    /// <summary>The fields a user may change on their own profile.</summary>
    public class UpdateProfileDto
    {
        [Required, StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? ProfilePictureUrl { get; set; }
    }
}