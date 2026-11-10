using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Outrally.Infrastructure;
using Xunit;

namespace Outrally.Tests;

public class CarpoolsControllerTests
{
    private HttpClient CreateClient(string userId = "user-1", string firstName = "Juan")
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("Jwt:Key", "test-secret-key-for-ci-only-32chars!!");
                builder.UseSetting("Jwt:Issuer", "outrally-api");
                builder.UseSetting("Jwt:Audience", "outrally-web");
                builder.UseSetting("ConnectionStrings:DefaultConnection", "");
                builder.ConfigureServices(services =>
                {
                    var descriptors = services
                        .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                                 || d.ServiceType == typeof(AppDbContext)).ToList();
                    foreach (var d in descriptors) services.Remove(d);
                    services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
                });
            });

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateToken(userId, firstName));
        return client;
    }

    private string GenerateToken(string userId, string firstName)
    {
        var key = new SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes("test-secret-key-for-ci-only-32chars!!"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "outrally-api",
            audience: "outrally-web",
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim("firstName", firstName)
            },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task GetCarpools_ReturnsOk()
    {
        var response = await CreateClient().GetAsync("/api/activities/1/carpools");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateOffer_ReturnsCreated()
    {
        var client = CreateClient();

        // seed activity via activities endpoint first
        var activity = new
        {
            Title = "Test Hike", Description = "Test", ScheduledDate = "2026-12-01T08:00:00",
            Location = "Park", MaxParticipants = 10
        };
        await client.PostAsJsonAsync("/api/activities", activity);

        var offer = new
        {
            TotalSeats = 3,
            DepartureLocation = "Transmountain Trailhead",
            DepartureTime = "2026-12-01T07:00:00",
            Notes = "Meet at east lot"
        };
        var response = await client.PostAsJsonAsync("/api/activities/1/carpools", offer);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task JoinCarpool_ReturnsOk()
    {
        var driver = CreateClient("driver-1", "Maria");
        var passenger = CreateClient("passenger-1", "Carlos");

        var activity = new
        {
            Title = "Hike", Description = "D", ScheduledDate = "2026-12-01T08:00:00",
            Location = "Park", MaxParticipants = 10
        };
        await driver.PostAsJsonAsync("/api/activities", activity);

        var offer = new
        {
            TotalSeats = 3, DepartureLocation = "Start", DepartureTime = "2026-12-01T07:00:00", Notes = ""
        };
        var created = await driver.PostAsJsonAsync("/api/activities/1/carpools", offer);
        var json = await created.Content.ReadAsStringAsync();
        var offerId = System.Text.Json.JsonDocument.Parse(json).RootElement.GetProperty("id").GetInt32();

        var response = await passenger.PostAsJsonAsync($"/api/carpools/{offerId}/join", new { });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}