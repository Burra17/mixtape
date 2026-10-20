using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Mixtape.Api.Hubs;
using Mixtape.Application.Rooms;

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
        connection.On(nameof(IGameClient.Pong), () => pong.TrySetResult());
        await connection.StartAsync();

        await connection.InvokeAsync(nameof(GameHub.Ping));

        // Ett uteblivet svar ger ett rött test efter fem sekunder i stället för ett test som hänger.
        await pong.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task CreateRoom_ThenResumeRoom_ReturnsTheSameRoom()
    {
        await using var connection = CreateConnection();
        await connection.StartAsync();

        var created = await connection.InvokeAsync<HubResponse<HostSession>>(nameof(GameHub.CreateRoom), null);
        var resumed = await connection.InvokeAsync<HubResponse<HostSession>>(nameof(GameHub.ResumeRoom), created.Value);

        Assert.That(created.Error, Is.Null);
        Assert.That(created.Value!.RoomCode, Has.Length.EqualTo(4));
        Assert.That(resumed.Value, Is.EqualTo(created.Value));
    }

    [Test]
    public async Task ResumeRoom_WithWrongToken_ReturnsTheErrorCode()
    {
        await using var connection = CreateConnection();
        await connection.StartAsync();

        var created = await connection.InvokeAsync<HubResponse<HostSession>>(nameof(GameHub.CreateRoom), null);
        var resumed = await connection.InvokeAsync<HubResponse<HostSession>>(nameof(GameHub.ResumeRoom), created.Value! with { HostToken = "fel" });

        Assert.That(resumed.Value, Is.Null);
        Assert.That(resumed.Error?.Code, Is.EqualTo("room.not_found"));
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
