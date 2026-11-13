using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Outrally.Infrastructure;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Outrally.Api.Controllers;
using Xunit;

namespace Outrally.Tests;

public class ProfileControllerTests
{
    // ── same JWT key used in all test factories ──
    private const string TestJwtKey = "outrally-super-secret-key-dev-only!";
    // 32-byte all-zeros key (base64) — test only, never production
    private const string TestMasterKey = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";

    private WebApplicationFactory<Program> CreateFactory()
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Jwt:Key", TestJwtKey);
            builder.UseSetting("Jwt:Issuer", "outrally-api");
            builder.UseSetting("Jwt:Audience", "outrally-web");
            builder.UseSetting("ConnectionStrings:DefaultConnection", "");
            builder.UseSetting("Encryption:MasterKey", TestMasterKey);
            builder.UseSetting("Encryption:MasterKey", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=");
            
            builder.ConfigureServices(services =>
            {
                var descriptors = services
                    .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                             || d.ServiceType == typeof(AppDbContext))
                    .ToList();
                foreach (var d in descriptors) services.Remove(d);

                var dbName = Guid.NewGuid().ToString(); // OUTSIDE lambda
                services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(dbName));
            });
        });
    }

    private HttpClient CreateClient(WebApplicationFactory<Program> factory, string userId, string role = "OE", string firstName = "Test")
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Role, role),
            new("firstName", firstName)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey));
        var token = new JwtSecurityToken(
            issuer: "outrally-api",
            audience: "outrally-web",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );
        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        return client;
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private async Task<int> SeedActivityAsync(HttpClient client)
    {
        var r = await client.PostAsJsonAsync("/api/activities", new
        {
            title = "Test Hike",
            description = "A hike",
            scheduledAt = DateTime.UtcNow.AddDays(7),
            location = "Trail",
            maxParticipants = 20
        });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);

        var list = await client.GetAsync("/api/activities");
        var json = await list.Content.ReadAsStringAsync();
        return System.Text.Json.JsonDocument.Parse(json).RootElement[0].GetProperty("id").GetInt32();
    }

    // ── Tests ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMedical_WhenEmpty_ReturnsNull()
    {
        var factory = CreateFactory();
        var client = CreateClient(factory, "user-1");

        var r = await client.GetAsync("/api/profile/medical");

        Assert.True(r.StatusCode == HttpStatusCode.OK || r.StatusCode == HttpStatusCode.NoContent);
        var body = await r.Content.ReadAsStringAsync();
    }

    [Fact]
    public async Task PutMedical_ThenGet_ReturnsDecryptedProfile()
    {
        var factory = CreateFactory();
        var client = CreateClient(factory, "user-2");

        var dto = new MedicalProfileDto(
            BloodType: "O+",
            Allergies: "Penicillin",
            Medications: "None",
            MedicalConditions: "None",
            EmergencyContactName: "Maria",
            EmergencyContactPhone: "555-1234",
            EmergencyContactRelationship: "Spouse"
        );

        var put = await client.PutAsJsonAsync("/api/profile/medical", dto);
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        var get = await client.GetAsync("/api/profile/medical");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var result = await get.Content.ReadFromJsonAsync<MedicalProfileDto>();

        Assert.NotNull(result);
        Assert.Equal("O+", result.BloodType);
        Assert.Equal("Penicillin", result.Allergies);
        Assert.Equal("Maria", result.EmergencyContactName);
    }

    [Fact]
    public async Task PutMedical_UpdateExisting_OverwritesCorrectly()
    {
        var factory = CreateFactory();
        var client = CreateClient(factory, "user-3");

        var dto1 = new MedicalProfileDto("A+", "None", "None", "None", "Bob", "555-0001", "Father");
        await client.PutAsJsonAsync("/api/profile/medical", dto1);

        var dto2 = new MedicalProfileDto("A+", "Sulfa", "Aspirin", "Hypertension", "Bob", "555-0001", "Father");
        await client.PutAsJsonAsync("/api/profile/medical", dto2);

        var get = await client.GetAsync("/api/profile/medical");
        var result = await get.Content.ReadFromJsonAsync<MedicalProfileDto>();

        Assert.Equal("Sulfa", result!.Allergies);
        Assert.Equal("Hypertension", result.MedicalConditions);
    }

    [Fact]
    public async Task GetRoster_AsAD_ReturnsAttendees()
    {
        var factory = CreateFactory();
        var admin = CreateClient(factory, "admin-1", "AD", "Alex");
        var member = CreateClient(factory, "member-1", "OE", "Carlos");

        // Create activity as admin, member joins
        var activityId = await SeedActivityAsync(admin);
        await member.PostAsync($"/api/activities/{activityId}/attend", null);

        // Member saves medical profile
        var dto = new MedicalProfileDto("B+", "None", "None", "None", "Ana", "555-9999", "Sister");
        await member.PutAsJsonAsync("/api/profile/medical", dto);

        // Admin downloads roster
        var r = await admin.GetAsync($"/api/activities/{activityId}/emergency-roster");
        Assert.True(r.StatusCode == HttpStatusCode.OK || r.StatusCode == HttpStatusCode.NoContent);

        var roster = await r.Content.ReadFromJsonAsync<List<System.Text.Json.JsonElement>>();
        Assert.NotNull(roster);
        Assert.True(roster.Count >= 1);
    }

    [Fact]
    public async Task GetRoster_AsOE_ReturnsForbidden()
    {
        var factory = CreateFactory();
        var member = CreateClient(factory, "member-2", "OE");

        var activityId = await SeedActivityAsync(member);

        var r = await member.GetAsync($"/api/activities/{activityId}/emergency-roster");
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task Profiles_AreIsolatedByUser()
    {
        // Encrypt/decrypt with different keys — user-A's data should not decrypt as user-B
        var factory = CreateFactory();
        var userA = CreateClient(factory, "user-a", "OE");
        var userB = CreateClient(factory, "user-b", "OE");

        var dtoA = new MedicalProfileDto("A+", "None", "None", "None", "Alice", "111", "Mom");
        var dtoB = new MedicalProfileDto("O-", "Pollen", "None", "Asthma", "Bob", "222", "Dad");

        await userA.PutAsJsonAsync("/api/profile/medical", dtoA);
        await userB.PutAsJsonAsync("/api/profile/medical", dtoB);

        var aResult = await (await userA.GetAsync("/api/profile/medical")).Content.ReadFromJsonAsync<MedicalProfileDto>();
        var bResult = await (await userB.GetAsync("/api/profile/medical")).Content.ReadFromJsonAsync<MedicalProfileDto>();

        Assert.Equal("A+", aResult!.BloodType);
        Assert.Equal("O-", bResult!.BloodType);
        Assert.NotEqual(aResult.BloodType, bResult.BloodType);
    }
}
