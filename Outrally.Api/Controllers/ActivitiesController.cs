using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Outrally.Core;
using Outrally.Infrastructure;

namespace Outrally.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ActivitiesController : ControllerBase
{
    private readonly AppDbContext _db;
    public ActivitiesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var activities = await _db.Activities.ToListAsync();
        var attendeeIds = await _db.ActivityAttendees
            .Where(a => a.UserId == userId && !a.Cancelled)
            .Select(a => a.ActivityId)
            .ToListAsync();

        var result = activities.Select(a => new
        {
            a.Id, a.Title, a.Description, a.ScheduledDate,
            a.Location, a.MaxParticipants, a.Status, a.CreatedAt,
            AttendeeCount = _db.ActivityAttendees.Count(att => att.ActivityId == a.Id && !att.Cancelled),
            IsAttending = attendeeIds.Contains(a.Id)
        });

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var activity = await _db.Activities.FindAsync(id);
        return activity is null ? NotFound() : Ok(activity);
    }

    [HttpPost]
    [Authorize(Roles = "AD,TL")]
    public async Task<IActionResult> Create(Activity activity)
    {
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = activity.Id }, activity);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var activity = await _db.Activities.FindAsync(id);
        if (activity is null) return NotFound();
        _db.Activities.Remove(activity);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/attend")]
    public async Task<IActionResult> Attend(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var activity = await _db.Activities.FindAsync(id);
        if (activity is null) return NotFound();

        var attendeeCount = await _db.ActivityAttendees
            .CountAsync(a => a.ActivityId == id && !a.Cancelled);
        if (attendeeCount >= activity.MaxParticipants)
            return BadRequest(new { message = "Activity is full." });

        var existing = await _db.ActivityAttendees
            .FirstOrDefaultAsync(a => a.ActivityId == id && a.UserId == userId);

        if (existing is not null)
        {
            existing.Cancelled = false;
        }
        else
        {
            _db.ActivityAttendees.Add(new ActivityAttendee
            {
                ActivityId = id,
                UserId = userId
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Joined successfully." });
    }

    [HttpDelete("{id}/attend")]
    public async Task<IActionResult> CancelAttendance(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var attendee = await _db.ActivityAttendees
            .FirstOrDefaultAsync(a => a.ActivityId == id && a.UserId == userId && !a.Cancelled);

        if (attendee is null) return NotFound();
        attendee.Cancelled = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
