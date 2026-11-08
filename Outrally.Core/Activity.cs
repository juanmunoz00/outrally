namespace Outrally.Core;

public class Activity
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime ScheduledDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public int MaxParticipants { get; set; }
    public ActivityStatus Status { get; set; } = ActivityStatus.Upcoming;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum ActivityStatus
{
    Upcoming,
    InProgress,
    Completed,
    Cancelled
}
