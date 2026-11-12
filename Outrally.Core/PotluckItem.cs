namespace Outrally.Core;

public enum PotluckUnit { Package, Bottle, Can, Bag, Box, Other }

public class PotluckItem
{
    public int Id { get; set; }
    public int ActivityId { get; set; }
    public Activity? Activity { get; set; }
    public string Name { get; set; } = string.Empty;
    public PotluckUnit Unit { get; set; } = PotluckUnit.Other;
    public int QuantityNeeded { get; set; }
    public string? Notes { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<PotluckClaim> Claims { get; set; } = new List<PotluckClaim>();
}