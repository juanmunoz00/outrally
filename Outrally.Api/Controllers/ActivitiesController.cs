using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
}
