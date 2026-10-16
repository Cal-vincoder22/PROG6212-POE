using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public class EventDto
{
    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    public DateTime EventDate { get; set; }

    [Required, StringLength(200)]
    public string Location { get; set; } = string.Empty;

    [Range(0.01, 1000)]
    public decimal DistanceKm { get; set; }

    /// <summary>Must be exactly "Run", "Walk" or "Cycle".</summary>
    [Required, RegularExpression("^(Run|Walk|Cycle)$", ErrorMessage = "EventType must be Run, Walk or Cycle.")]
    public string EventType { get; set; } = string.Empty;
}

public class CategoryDto
{
    [Required, StringLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }

    [Range(0.01, 1000)]
    public decimal DistanceKm { get; set; }
}