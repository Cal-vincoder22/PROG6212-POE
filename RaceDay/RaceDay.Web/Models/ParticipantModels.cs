using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models
{
    public class CategoryItem
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        public decimal? DistanceKm { get; set; }
    }

    public class EventDetailModel
    {
        public int EventId { get; set; }
        public int OrganiserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string? BannerImageUrl { get; set; }
        public string OrganiserName { get; set; } = string.Empty;
        public List<CategoryItem> Categories { get; set; } = new();
    }

    public class EnrolmentItem
    {
        public int EnrolmentId { get; set; }
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public DateTime EnrolmentDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
    }

    public class ResultItem
    {
        public int ResultId { get; set; }
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public TimeSpan FinishTime { get; set; }
        public int FinishPosition { get; set; }
        public int TotalFinishers { get; set; }

        public string PositionText =>
            TotalFinishers > 0
                ? $"{Ordinal(FinishPosition)} out of {TotalFinishers} finishers"
                : Ordinal(FinishPosition);

        private static string Ordinal(int n)
        {
            var suffix = (n % 100) is 11 or 12 or 13
                ? "th"
                : (n % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
            return n + suffix;
        }
    }

    public class ProfileViewModel
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "Please enter your name."), StringLength(100)]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
    }
}