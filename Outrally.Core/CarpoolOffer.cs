namespace Outrally.Core;

public class CarpoolOffer
{
    public int Id { get; set; }
    public int ActivityId { get; set; }
    public Activity? Activity { get; set; }
    public string DriverUserId { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public int TotalSeats { get; set; }
    public string DepartureLocation { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<CarpoolPassenger> Passengers { get; set; } = new List<CarpoolPassenger>();
}