using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos
{
    /// <summary>Request body for enrolling in a category.</summary>
    public class CreateEnrolmentDto
    {
        [Required]
        public int CategoryId { get; set; }
    }

    /// <summary>An enrolment as shown to a participant.</summary>
    public class EnrolmentDto
    {
        public int EnrolmentId { get; set; }
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public DateTime EnrolmentDate { get; set; }
    }

    /// <summary>An enrolment as shown to the event's organiser.</summary>
    public class EventEnrolmentDto
    {
        public int EnrolmentId { get; set; }
        public int UserId { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public string ParticipantEmail { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public DateTime EnrolmentDate { get; set; }
    }
}