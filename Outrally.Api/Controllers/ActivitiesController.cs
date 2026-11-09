using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Outrally.Core;
using Outrally.Infrastructure;

namespace Outrally.Api.Controllers;

[ApiController]
[Route("api/activities")]
public class ActivitiesController : ControllerBase
{
    private readonly AppDbContext _db;

    public ActivitiesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var activities = await _db.Activities.ToListAsync();
        return Ok(activities);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var activity = await _db.Activities.FindAsync(id);
        if (activity == null) return NotFound();
        return Ok(activity);
    }

    [HttpPost]
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
        if (activity == null) return NotFound();
        _db.Activities.Remove(activity);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
