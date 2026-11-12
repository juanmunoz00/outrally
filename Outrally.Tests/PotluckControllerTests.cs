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
using Outrally.Core;
using Outrally.Infrastructure;
using Xunit;

namespace Outrally.Tests;

public class PotluckControllerTests
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
                builder.ConfigureServices(services =>
                {
                    var descriptors = services
                        .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                                 || d.ServiceType == typeof(AppDbContext)).ToList();
                    foreach (var d in descriptors) services.Remove(d);
                    var dbName = Guid.NewGuid().ToString();
                    services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(dbName));
                });
            });
    }

    private HttpClient CreateClient(WebApplicationFactory<Program> factory,
        string userId = "user-1", string firstName = "Juan", string role = "TL")
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateToken(userId, firstName, role));
        return client;
    }

    private string GenerateToken(string userId, string firstName, string role)
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
                new Claim("firstName", firstName),
                new Claim(ClaimTypes.Role, role) 
            },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<int> SeedActivityAsync(HttpClient client)
    {
        var activity = new
        {
            Title = "Turtle Rock", Description = "Cookout",
            ScheduledDate = "2026-12-01T08:00:00",
            Location = "Turtle Rock Trail", MaxParticipants = 20
        };
        var r = await client.PostAsJsonAsync("/api/activities", activity);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);

        var list = await client.GetAsync("/api/activities");
        var json = await list.Content.ReadAsStringAsync();
        return System.Text.Json.JsonDocument.Parse(json).RootElement[0].GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task GetPotluck_ReturnsOk()
    {
        var factory = CreateFactory();
        var client = CreateClient(factory);
        var activityId = await SeedActivityAsync(client);

        var response = await client.GetAsync($"/api/activities/{activityId}/potluck");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddItem_TL_ReturnsCreated()
    {
        var factory = CreateFactory();
        var client = CreateClient(factory, role: "TL");
        var activityId = await SeedActivityAsync(client);

        var item = new { Name = "Hotdog buns", Unit = 0, QuantityNeeded = 3, Notes = "" };
        var response = await client.PostAsJsonAsync($"/api/activities/{activityId}/potluck", item);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task AddItem_OE_ReturnsForbid()
    {
        var factory = CreateFactory();
        var tlClient = CreateClient(factory, "tl-1", "Maria", "TL");
        var oeClient = CreateClient(factory, "oe-1", "Carlos", "OE");
        var activityId = await SeedActivityAsync(tlClient);

        var item = new { Name = "Chips", Unit = 3, QuantityNeeded = 2, Notes = "" };
        var response = await oeClient.PostAsJsonAsync($"/api/activities/{activityId}/potluck", item);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ClaimItem_ReturnsOk()
    {
        var factory = CreateFactory();
        var tl = CreateClient(factory, "tl-1", "Maria", "TL");
        var oe = CreateClient(factory, "oe-1", "Carlos", "OE");
        var activityId = await SeedActivityAsync(tl);

        var item = new { Name = "Soda", Unit = 4, QuantityNeeded = 2, Notes = "" };
        var created = await tl.PostAsJsonAsync($"/api/activities/{activityId}/potluck", item);
        var itemId = System.Text.Json.JsonDocument.Parse(await created.Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetInt32();

        var response = await oe.PostAsJsonAsync($"/api/potluck/items/{itemId}/claim",
            new { QuantityClaimed = 1 });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}