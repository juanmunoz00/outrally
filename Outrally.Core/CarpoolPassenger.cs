namespace Outrally.Core;

public class CarpoolPassenger
{
    public int Id { get; set; }
    public int CarpoolOfferId { get; set; }
    public CarpoolOffer? CarpoolOffer { get; set; }
    public string PassengerId { get; set; } = string.Empty;
    public string PassengerName { get; set; } = string.Empty;
    public DateTime SignedUpAt { get; set; } = DateTime.UtcNow;
}