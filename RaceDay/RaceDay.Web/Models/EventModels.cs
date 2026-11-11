namespace RaceDay.Web.Models
{
    public class EventListItem
    {
        public int EventId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string? BannerImageUrl { get; set; }
        public string OrganiserName { get; set; } = string.Empty;
        public int EnrolmentCount { get; set; }
    }

    public class HomeViewModel
    {
        public List<EventListItem> Events { get; set; } = new();
        public string? EventType { get; set; }
        public bool UpcomingOnly { get; set; } = true;
        public string? Error { get; set; }
    }
}