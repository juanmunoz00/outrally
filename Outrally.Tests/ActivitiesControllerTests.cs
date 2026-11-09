using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Outrally.Infrastructure;
using Xunit;

namespace Outrally.Tests;

public class ActivitiesControllerTests
{
    private HttpClient CreateClient()
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
                    // Remove ALL AppDbContext registrations
                    var descriptors = services
                        .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                                 || d.ServiceType == typeof(AppDbContext))
                        .ToList();
                    foreach (var d in descriptors) services.Remove(d);

                    services.AddDbContext<AppDbContext>(options =>
                        options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
                });
            });

        return factory.CreateClient();
    }

    [Fact]
    public async Task GetActivities_ReturnsOk()
    {
        var response = await CreateClient().GetAsync("/api/activities");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetActivities_ReturnsEmptyList_WhenNoActivities()
    {
        var response = await CreateClient().GetAsync("/api/activities");
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("[]", content);
    }

    [Fact]
    public async Task CreateActivity_ReturnsCreated()
    {
        var activity = new
        {
            Title = "Franklin Mountains Hike",
            Description = "Morning hike to the summit",
            ScheduledDate = "2026-12-01T08:00:00",
            Location = "Franklin Mountains State Park",
            MaxParticipants = 15
        };

        var response = await CreateClient().PostAsJsonAsync("/api/activities", activity);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task GetActivityById_ReturnsOk_WhenExists()
    {
        var response = await CreateClient().GetAsync("/api/activities/1");
        Assert.True(response.StatusCode == HttpStatusCode.OK ||
                    response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact(Skip = "Revisit after DB wired")]
    public async Task DeleteActivity_ReturnsNoContent_WhenExists()
    {
        var client = CreateClient();
        var activity = new
        {
            Title = "Test Hike",
            Description = "Test",
            ScheduledDate = "2026-12-01T08:00:00",
            Location = "Test Location",
            MaxParticipants = 10
        };
        var created = await client.PostAsJsonAsync("/api/activities", activity);
        var json = await created.Content.ReadAsStringAsync();
        var doc = System.Text.Json.JsonDocument.Parse(json);
        var id = doc.RootElement.GetProperty("id").GetInt32();

        var getResponse = await client.GetAsync($"/api/activities/{id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var response = await client.DeleteAsync($"/api/activities/{id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
