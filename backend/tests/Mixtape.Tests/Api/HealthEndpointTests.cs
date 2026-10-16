using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Mixtape.Tests.Api;

public sealed class HealthEndpointTests
{
    private WebApplicationFactory<Program> _factory = null!;

    // En värd per testklass räcker, eftersom /health inte har något state som testerna kan störa.
    [OneTimeSetUp]
    public void StartApi()
    {
        _factory = new WebApplicationFactory<Program>();
    }

    [OneTimeTearDown]
    public void StopApi()
    {
        _factory.Dispose();
    }

    [Test]
    public async Task GetHealth_ReturnsOk()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/health");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}
