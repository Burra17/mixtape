using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;

namespace Mixtape.Tests.Api;

public sealed class GameHubTests
{
    private WebApplicationFactory<Program> _factory = null!;

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
    public async Task Ping_RepliesWithPongToCaller()
    {
        await using var connection = CreateConnection();
        var pong = new TaskCompletionSource();
        connection.On("Pong", () => pong.TrySetResult());
        await connection.StartAsync();

        await connection.InvokeAsync("Ping");

        // WaitAsync gör att ett uteblivet svar ger ett rött test i stället för ett test som hänger.
        Assert.That(async () => await pong.Task.WaitAsync(TimeSpan.FromSeconds(5)), Throws.Nothing);
    }

    // Testservern har ingen riktig port, så klienten går via dess HTTP-hanterare med long polling.
    private HubConnection CreateConnection()
    {
        var server = _factory.Server;
        return new HubConnectionBuilder()
            .WithUrl(new Uri(server.BaseAddress, "gamehub"), HttpTransportType.LongPolling, options =>
            {
                options.HttpMessageHandlerFactory = _ => server.CreateHandler();
            })
            .Build();
    }
}
