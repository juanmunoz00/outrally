namespace Outrally.Core;

public class ActivityAttendee
{
    public int Id { get; set; }
    public int ActivityId { get; set; }
    public Activity? Activity { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime SignedUpAt { get; set; } = DateTime.UtcNow;
    public bool Cancelled { get; set; } = false;
}
