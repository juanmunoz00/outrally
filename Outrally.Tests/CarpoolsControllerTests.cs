using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Outrally.Infrastructure;
using Xunit;

namespace Outrally.Tests;

public class CarpoolsControllerTests
{
    private const string TestJwtKey = "test-secret-key-for-ci-only-32chars!!";

    private WebApplicationFactory<Program> CreateFactory()
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("Jwt:Key", TestJwtKey);
                builder.UseSetting("Jwt:Issuer", "outrally-api");
                builder.UseSetting("Jwt:Audience", "outrally-web");
                builder.UseSetting("ConnectionStrings:DefaultConnection", "");
                builder.UseSetting("Encryption:MasterKey", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=");
                
                builder.ConfigureServices(services =>
                {
                    var descriptors = services
                        .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                                || d.ServiceType == typeof(AppDbContext)).ToList();
                    foreach (var d in descriptors) services.Remove(d);

                    var dbName = Guid.NewGuid().ToString();   // ← fix
                    services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(dbName));
                });
            });
    }

    private HttpClient CreateClient(WebApplicationFactory<Program> factory, string userId = "user-1", string firstName = "Juan")
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateToken(userId, firstName));
        return client;
    }

    private string GenerateToken(string userId, string firstName)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "outrally-api",
            audience: "outrally-web",
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.Email, $"{userId}@test.com"),
                new Claim("firstName", firstName)
            },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Helper: creates an activity and returns its actual ID via GET
    private async Task<int> SeedActivityAsync(HttpClient client)
    {
        var activity = new
        {
            Title = "Test Hike", Description = "Test",
            ScheduledDate = "2026-12-01T08:00:00",
            Location = "Park", MaxParticipants = 10
        };
        var createResponse = await client.PostAsJsonAsync("/api/activities", activity);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        // Fetch via GET to get the real assigned ID
        var listResponse = await client.GetAsync("/api/activities");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var json = await listResponse.Content.ReadAsStringAsync();
        var doc = System.Text.Json.JsonDocument.Parse(json);
        return doc.RootElement[0].GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task GetCarpools_ReturnsOk()
    {
        var factory = CreateFactory();
        var client = CreateClient(factory);
        var activityId = await SeedActivityAsync(client);

        var response = await client.GetAsync($"/api/activities/{activityId}/carpools");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateOffer_ReturnsCreated()
    {
        var factory = CreateFactory();
        var client = CreateClient(factory);
        var activityId = await SeedActivityAsync(client);

        var offer = new
        {
            TotalSeats = 3,
            DepartureLocation = "Transmountain Trailhead",
            DepartureTime = "2026-12-01T07:00:00",
            Notes = "Meet at east lot"
        };
        var response = await client.PostAsJsonAsync($"/api/activities/{activityId}/carpools", offer);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task JoinCarpool_ReturnsOk()
    {
        // Share one factory so both clients use the same InMemory DB
        var factory = CreateFactory();
        var driver = CreateClient(factory, "driver-1", "Maria");
        var passenger = CreateClient(factory, "passenger-1", "Carlos");

        var activityId = await SeedActivityAsync(driver);

        var offer = new
        {
            TotalSeats = 3,
            DepartureLocation = "Transmountain Trailhead",
            DepartureTime = "2026-12-01T07:00:00",
            Notes = ""
        };
        var offerResponse = await driver.PostAsJsonAsync($"/api/activities/{activityId}/carpools", offer);
        Assert.Equal(HttpStatusCode.Created, offerResponse.StatusCode);

        var offerJson = await offerResponse.Content.ReadAsStringAsync();
        var offerId = System.Text.Json.JsonDocument.Parse(offerJson)
            .RootElement.GetProperty("id").GetInt32();

        var response = await passenger.PostAsJsonAsync($"/api/carpools/{offerId}/join", new { });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}