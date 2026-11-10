using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Outrally.Core;
using Outrally.Infrastructure;

namespace Outrally.Api.Controllers;

[ApiController]
[Authorize]
public class CarpoolsController : ControllerBase
{
    private readonly AppDbContext _db;
    public CarpoolsController(AppDbContext db) => _db = db;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private string UserFirstName => User.FindFirstValue("firstName") ?? "Unknown";
// GET /api/activities/{activityId}/carpools
[HttpGet("api/activities/{activityId}/carpools")]
public async Task<IActionResult> GetCarpools(int activityId)
{
    var carpools = await _db.CarpoolOffers
        .Include(c => c.Passengers)
        .Where(c => c.ActivityId == activityId)
        .Select(c => new
        {
            c.Id,
            c.DriverUserId,
            c.DriverName,
            c.TotalSeats,
            SeatsAvailable = c.TotalSeats - c.Passengers.Count,
            c.DepartureLocation,
            c.DepartureTime,
            c.Notes,
            IsMyOffer = c.DriverUserId == UserId,
            IsMyRide = c.Passengers.Any(p => p.PassengerId == UserId),
            Passengers = c.Passengers.Select(p => p.PassengerName).ToList()
        })
        .ToListAsync();

    return Ok(carpools);
}

    // POST /api/activities/{activityId}/carpools
    [HttpPost("api/activities/{activityId}/carpools")]
    public async Task<IActionResult> CreateOffer(int activityId, [FromBody] CreateCarpoolDto dto)
    {
        var activity = await _db.Activities.FindAsync(activityId);
        if (activity == null) return NotFound();

        var already = await _db.CarpoolOffers
            .AnyAsync(c => c.ActivityId == activityId && c.DriverUserId == UserId);
        if (already) return Conflict("You already have a carpool offer for this activity.");

        var offer = new CarpoolOffer
        {
            ActivityId = activityId,
            DriverUserId = UserId,
            DriverName = UserFirstName,
            TotalSeats = dto.TotalSeats,
            DepartureLocation = dto.DepartureLocation,
            DepartureTime = dto.DepartureTime,
            Notes = dto.Notes
        };

        _db.CarpoolOffers.Add(offer);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetCarpools), new { activityId }, new { offer.Id });
    }

// POST /api/carpools/{offerId}/join
[HttpPost("api/carpools/{offerId}/join")]
public async Task<IActionResult> JoinCarpool(int offerId)
{
    var offer = await _db.CarpoolOffers
        .Include(c => c.Passengers)
        .FirstOrDefaultAsync(c => c.Id == offerId);

    if (offer == null) return NotFound();
    if (offer.DriverUserId == UserId) return Conflict("You are the driver.");
    if (offer.Passengers.Any(p => p.PassengerId == UserId)) return Conflict("Already in this carpool.");
    if (offer.Passengers.Count >= offer.TotalSeats) return Conflict("No seats available.");

    // Drop from any other carpool for the same activity (ride switch)
    var existing = await _db.CarpoolPassengers
        .Include(p => p.CarpoolOffer)
        .FirstOrDefaultAsync(p => p.PassengerId == UserId
                               && p.CarpoolOffer!.ActivityId == offer.ActivityId
                               && p.CarpoolOfferId != offerId);
    if (existing != null) _db.CarpoolPassengers.Remove(existing);

    offer.Passengers.Add(new CarpoolPassenger
    {
        PassengerId = UserId,
        PassengerName = UserFirstName
    });

    await _db.SaveChangesAsync();
    return Ok();
}

    // DELETE /api/carpools/{offerId}/join
    [HttpDelete("api/carpools/{offerId}/join")]
    public async Task<IActionResult> LeaveCarpool(int offerId)
    {
        var passenger = await _db.CarpoolPassengers
            .FirstOrDefaultAsync(p => p.CarpoolOfferId == offerId && p.PassengerId == UserId);

        if (passenger == null) return NotFound();

        _db.CarpoolPassengers.Remove(passenger);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // DELETE /api/carpools/{offerId} — driver cancels offer
    [HttpDelete("api/carpools/{offerId}")]
    public async Task<IActionResult> DeleteOffer(int offerId)
    {
        var offer = await _db.CarpoolOffers.FirstOrDefaultAsync(c => c.Id == offerId);
        if (offer == null) return NotFound();
        if (offer.DriverUserId != UserId) return Forbid();

        _db.CarpoolOffers.Remove(offer);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public record CreateCarpoolDto(int TotalSeats, string DepartureLocation, DateTime DepartureTime, string? Notes);