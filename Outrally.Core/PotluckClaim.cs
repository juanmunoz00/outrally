namespace Outrally.Core;

public class PotluckClaim
{
    public int Id { get; set; }
    public int PotluckItemId { get; set; }
    public PotluckItem? PotluckItem { get; set; }
    public string ClaimantUserId { get; set; } = string.Empty;
    public string ClaimantName { get; set; } = string.Empty;
    public int QuantityClaimed { get; set; }
    public DateTime ClaimedAt { get; set; } = DateTime.UtcNow;
}