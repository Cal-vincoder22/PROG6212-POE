using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models
{
    public class OrganiserEventItem
    {
        public int EventId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string? BannerImageUrl { get; set; }
        public int EnrolmentCount { get; set; }
    }

    public class EventFormModel
    {
        public int EventId { get; set; }

        [Required(ErrorMessage = "Please enter an event name."), StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Please choose a date.")]
        [Display(Name = "Date and time")]
        public DateTime EventDate { get; set; } = DateTime.Today.AddDays(30);

        [Required(ErrorMessage = "Please enter a location."), StringLength(200)]
        public string Location { get; set; } = string.Empty;

        [Range(0.1, 1000, ErrorMessage = "Distance must be between 0.1 and 1000 km.")]
        [Display(Name = "Distance (km)")]
        public decimal DistanceKm { get; set; } = 10;

        [Required]
        [Display(Name = "Event type")]
        public string EventType { get; set; } = "Run";
    }

    public class CategoryFormModel
    {
        [Required(ErrorMessage = "Please enter a category name."), StringLength(100)]
        public string CategoryName { get; set; } = string.Empty;
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        public decimal? DistanceKm { get; set; }
    }

    public class EventEnrolmentItem
    {
        public int EnrolmentId { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public DateTime EnrolmentDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}