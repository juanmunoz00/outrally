using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Outrally.Core;
using Outrally.Infrastructure;
using System.Security.Claims;
using System.Text.Json;

namespace Outrally.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/profile")]
public class ProfileController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IEncryptionService _encryption;
    private readonly UserManager<ApplicationUser> _userManager;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private string UserRole => User.FindFirstValue(ClaimTypes.Role) ?? "OE";

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public ProfileController(AppDbContext db, IEncryptionService encryption, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _encryption = encryption;
        _userManager = userManager;
    }

    // GET /api/profile/medical
    [HttpGet("medical")]
    public async Task<IActionResult> GetMedical()
    {
        var profile = await _db.MedicalProfiles.FirstOrDefaultAsync(p => p.UserId == UserId);
        if (profile == null || profile.EncryptedData.Length == 0) return Ok(null);

        var json = _encryption.Decrypt(profile.EncryptedData, UserId);
        return Ok(JsonSerializer.Deserialize<MedicalProfileDto>(json, JsonOpts));
    }

    // PUT /api/profile/medical
    [HttpPut("medical")]
    public async Task<IActionResult> PutMedical([FromBody] MedicalProfileDto dto)
    {
        var encrypted = _encryption.Encrypt(JsonSerializer.Serialize(dto, JsonOpts), UserId);

        var profile = await _db.MedicalProfiles.FirstOrDefaultAsync(p => p.UserId == UserId);
        if (profile == null)
        {
            profile = new MedicalProfile { UserId = UserId };
            _db.MedicalProfiles.Add(profile);
        }

        profile.EncryptedData = encrypted;
        profile.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(dto);
    }

    // GET /api/activities/{activityId}/emergency-roster  — AD only
    [HttpGet("/api/activities/{activityId}/emergency-roster")]
    public async Task<IActionResult> GetRoster(int activityId)
    {
        if (UserRole != "AD") return Forbid();

        var attendees = await _db.ActivityAttendees
            .Where(a => a.ActivityId == activityId)
            .ToListAsync();

        var roster = new List<RosterEntryDto>();
        foreach (var attendee in attendees)
        {
            var user = await _userManager.FindByIdAsync(attendee.UserId);
            MedicalProfileDto? med = null;

            var mp = await _db.MedicalProfiles.FirstOrDefaultAsync(p => p.UserId == attendee.UserId);
            if (mp != null && mp.EncryptedData.Length > 0)
            {
                var json = _encryption.Decrypt(mp.EncryptedData, attendee.UserId);
                med = JsonSerializer.Deserialize<MedicalProfileDto>(json, JsonOpts);
            }

            roster.Add(new RosterEntryDto(
                attendee.UserId,
                user?.FirstName ?? "",
                user?.LastName ?? "",
                user?.Email ?? "",
                med
            ));
        }

        return Ok(roster);
    }
}

public record MedicalProfileDto(
    string BloodType,
    string Allergies,
    string Medications,
    string MedicalConditions,
    string EmergencyContactName,
    string EmergencyContactPhone,
    string EmergencyContactRelationship
);

public record RosterEntryDto(
    string UserId,
    string FirstName,
    string LastName,
    string Email,
    MedicalProfileDto? MedicalProfile
);
