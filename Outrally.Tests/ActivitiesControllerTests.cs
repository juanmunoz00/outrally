using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Outrally.Tests;

public class ActivitiesControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ActivitiesControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetActivities_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/activities");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetActivities_ReturnsEmptyList_WhenNoActivities()
    {
        var response = await _client.GetAsync("/api/activities");
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("[]", content);
    }
}
