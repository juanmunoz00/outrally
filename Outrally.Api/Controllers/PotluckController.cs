using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Outrally.Core;
using Outrally.Infrastructure;

namespace Outrally.Api.Controllers;

[ApiController]
[Authorize]
public class PotluckController : ControllerBase
{
    private readonly AppDbContext _db;
    public PotluckController(AppDbContext db) => _db = db;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private string UserName => User.FindFirstValue("firstName") ?? "Unknown";
    private string UserRole => User.FindFirstValue(ClaimTypes.Role) ?? "OE";
    private bool IsTlOrAd => UserRole is "TL" or "AD";

    // GET /api/activities/{activityId}/potluck
    [HttpGet("api/activities/{activityId}/potluck")]
    public async Task<IActionResult> GetItems(int activityId)
    {
        var items = await _db.PotluckItems
            .Include(i => i.Claims)
            .Where(i => i.ActivityId == activityId)
            .Select(i => new
            {
                i.Id,
                i.Name,
                i.Unit,
                i.QuantityNeeded,
                i.Notes,
                i.CreatedByUserId,
                QuantityClaimed = i.Claims.Sum(c => c.QuantityClaimed),
                QuantityRemaining = i.QuantityNeeded - i.Claims.Sum(c => c.QuantityClaimed),
                IsFullyClaimed = i.Claims.Sum(c => c.QuantityClaimed) >= i.QuantityNeeded,
                IsMyItem = i.CreatedByUserId == UserId,
                MyClaim = i.Claims
                    .Where(c => c.ClaimantUserId == UserId)
                    .Select(c => new { c.Id, c.QuantityClaimed })
                    .FirstOrDefault(),
                Claims = i.Claims.Select(c => new
                {
                    c.Id,
                    c.ClaimantName,
                    c.QuantityClaimed
                }).ToList()
            })
            .ToListAsync();

        return Ok(items);
    }

    // POST /api/activities/{activityId}/potluck — TL/AD only
    [HttpPost("api/activities/{activityId}/potluck")]
    public async Task<IActionResult> AddItem(int activityId, [FromBody] CreatePotluckItemDto dto)
    {
        if (!IsTlOrAd) return Forbid();

        var activity = await _db.Activities.FindAsync(activityId);
        if (activity == null) return NotFound();

        var item = new PotluckItem
        {
            ActivityId = activityId,
            Name = dto.Name,
            Unit = dto.Unit,
            QuantityNeeded = dto.QuantityNeeded,
            Notes = dto.Notes,
            CreatedByUserId = UserId
        };

        _db.PotluckItems.Add(item);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetItems), new { activityId }, new { item.Id });
    }

    // PUT /api/potluck/items/{id} — TL/AD only, must be creator
    [HttpPut("api/potluck/items/{id}")]
    public async Task<IActionResult> UpdateItem(int id, [FromBody] CreatePotluckItemDto dto)
    {
        if (!IsTlOrAd) return Forbid();

        var item = await _db.PotluckItems.FindAsync(id);
        if (item == null) return NotFound();
        if (item.CreatedByUserId != UserId) return Forbid();

        item.Name = dto.Name;
        item.Unit = dto.Unit;
        item.QuantityNeeded = dto.QuantityNeeded;
        item.Notes = dto.Notes;

        await _db.SaveChangesAsync();
        return Ok(new { item.Id });
    }

    // DELETE /api/potluck/items/{id} — TL/AD only, must be creator
    [HttpDelete("api/potluck/items/{id}")]
    public async Task<IActionResult> DeleteItem(int id)
    {
        if (!IsTlOrAd) return Forbid();

        var item = await _db.PotluckItems.FindAsync(id);
        if (item == null) return NotFound();
        if (item.CreatedByUserId != UserId) return Forbid();

        _db.PotluckItems.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // POST /api/potluck/items/{id}/claim — any authenticated user
    [HttpPost("api/potluck/items/{itemId}/claim")]
    public async Task<IActionResult> ClaimItem(int itemId, [FromBody] ClaimPotluckDto dto)
    {
        if (dto.QuantityClaimed <= 0) return BadRequest("Quantity must be at least 1.");

        var item = await _db.PotluckItems
            .Include(i => i.Claims)
            .FirstOrDefaultAsync(i => i.Id == itemId);

        if (item == null) return NotFound();

        var alreadyClaimed = item.Claims.Sum(c => c.QuantityClaimed);
        if (alreadyClaimed + dto.QuantityClaimed > item.QuantityNeeded)
            return Conflict($"Only {item.QuantityNeeded - alreadyClaimed} remaining.");

        var existing = item.Claims.FirstOrDefault(c => c.ClaimantUserId == UserId);
        if (existing != null)
        {
            existing.QuantityClaimed += dto.QuantityClaimed;
        }
        else
        {
            item.Claims.Add(new PotluckClaim
            {
                ClaimantUserId = UserId,
                ClaimantName = UserName,
                QuantityClaimed = dto.QuantityClaimed
            });
        }

        await _db.SaveChangesAsync();
        return Ok();
    }

    // DELETE /api/potluck/claims/{id} — claimant only
    [HttpDelete("api/potluck/claims/{id}")]
    public async Task<IActionResult> DropClaim(int id)
    {
        var claim = await _db.PotluckClaims.FindAsync(id);
        if (claim == null) return NotFound();
        if (claim.ClaimantUserId != UserId) return Forbid();

        _db.PotluckClaims.Remove(claim);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public record CreatePotluckItemDto(string Name, PotluckUnit Unit, int QuantityNeeded, string? Notes);
public record ClaimPotluckDto(int QuantityClaimed);