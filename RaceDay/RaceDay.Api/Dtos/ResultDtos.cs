using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos
{
    /// <summary>Request body for capturing or correcting a result.</summary>
    public class CaptureResultDto
    {
        /// <summary>Finish time in hh:mm:ss format, for example 01:45:30.</summary>
        [Required]
        public TimeSpan FinishTime { get; set; }

        [Range(1, int.MaxValue)]
        public int FinishPosition { get; set; }
    }

    /// <summary>A race result with its event and participant details.</summary>
    public class ResultDto
    {
        public int ResultId { get; set; }
        public int EnrolmentId { get; set; }
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string ParticipantName { get; set; } = string.Empty;
        public TimeSpan FinishTime { get; set; }
        public int FinishPosition { get; set; }
        public DateTime CapturedAt { get; set; }
    }
}