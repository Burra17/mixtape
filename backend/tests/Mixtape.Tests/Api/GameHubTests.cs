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

    [Test]
    public async Task JoinRoom_SendsLobbyToHost()
    {
        await using var host = CreateConnection();
        await using var player = CreateConnection();
        var lobbies = new List<LobbyState>();
        var annaJoined = new TaskCompletionSource();
        host.On<LobbyState>(nameof(IGameClient.LobbyUpdated), lobby =>
        {
            lobbies.Add(lobby);
            if (lobby.Players.Count == 1)
            {
                annaJoined.TrySetResult();
            }
        });
        await host.StartAsync();
        await player.StartAsync();

        var created = await host.InvokeAsync<HubResponse<HostSession>>(nameof(GameHub.CreateRoom), null);
        var joined = await player.InvokeAsync<HubResponse<PlayerSession>>(nameof(GameHub.JoinRoom), created.Value!.RoomCode, "Anna");
        await annaJoined.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.That(joined.Value!.PlayerToken, Has.Length.EqualTo(32));
        Assert.That(lobbies[0].Players, Is.Empty);
        Assert.That(lobbies[^1].Players.Single(), Is.EqualTo(new LobbyPlayer(joined.Value.PlayerId, "Anna", 1, true)));
    }

    [Test]
    public async Task JoinRoom_WithTakenName_ReturnsTheErrorCode()
    {
        await using var host = CreateConnection();
        await host.StartAsync();

        var created = await host.InvokeAsync<HubResponse<HostSession>>(nameof(GameHub.CreateRoom), null);
        await host.InvokeAsync<HubResponse<PlayerSession>>(nameof(GameHub.JoinRoom), created.Value!.RoomCode, "Anna");
        var second = await host.InvokeAsync<HubResponse<PlayerSession>>(nameof(GameHub.JoinRoom), created.Value.RoomCode, "anna");

        Assert.That(second.Error?.Code, Is.EqualTo("player.name_taken"));
    }

    [Test]
    public async Task RemovePlayer_TellsOnlyThatPlayer()
    {
        await using var host = CreateConnection();
        await using var anna = CreateConnection();
        await using var bertil = CreateConnection();
        var annaRemoved = new TaskCompletionSource();
        var bertilRemoved = false;
        anna.On(nameof(IGameClient.RemovedFromRoom), () => annaRemoved.TrySetResult());
        var bertilSawAnnaGo = new TaskCompletionSource();
        bertil.On(nameof(IGameClient.RemovedFromRoom), () => bertilRemoved = true);
        bertil.On<LobbyState>(nameof(IGameClient.LobbyUpdated), lobby =>
        {
            if (lobby.Players.Count == 1 && lobby.Players[0].Name == "Bertil")
            {
                bertilSawAnnaGo.TrySetResult();
            }
        });
        await Task.WhenAll(host.StartAsync(), anna.StartAsync(), bertil.StartAsync());

        var created = (await host.InvokeAsync<HubResponse<HostSession>>(nameof(GameHub.CreateRoom), null)).Value!;
        var annaSession = (await anna.InvokeAsync<HubResponse<PlayerSession>>(nameof(GameHub.JoinRoom), created.RoomCode, "Anna")).Value!;
        await bertil.InvokeAsync<HubResponse<PlayerSession>>(nameof(GameHub.JoinRoom), created.RoomCode, "Bertil");

        await host.InvokeAsync(nameof(GameHub.RemovePlayer), created, annaSession.PlayerId);
        await annaRemoved.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Lobbyn skickas efter RemovedFromRoom, så när Bertil sett den nya lobbyn hade ett felaktigt RemovedFromRoom redan kommit fram.
        await bertilSawAnnaGo.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.That(bertilRemoved, Is.False);
    }

    [Test]
    public async Task LeaveRoom_AfterBeingRemoved_StopsTheLobbyUpdates()
    {
        await using var host = CreateConnection();
        await using var anna = CreateConnection();
        await using var bertil = CreateConnection();
        var hostSawBertil = new TaskCompletionSource();
        var annaLobbies = 0;
        host.On<LobbyState>(nameof(IGameClient.LobbyUpdated), lobby =>
        {
            if (lobby.Players.Any(player => player.Name == "Bertil"))
            {
                hostSawBertil.TrySetResult();
            }
        });
        anna.On<LobbyState>(nameof(IGameClient.LobbyUpdated), _ => annaLobbies++);
        await Task.WhenAll(host.StartAsync(), anna.StartAsync(), bertil.StartAsync());

        var created = (await host.InvokeAsync<HubResponse<HostSession>>(nameof(GameHub.CreateRoom), null)).Value!;
        var annaSession = (await anna.InvokeAsync<HubResponse<PlayerSession>>(nameof(GameHub.JoinRoom), created.RoomCode, "Anna")).Value!;
        await host.InvokeAsync(nameof(GameHub.RemovePlayer), created, annaSession.PlayerId);

        // Som frontenden gör när RemovedFromRoom kommer.
        await anna.InvokeAsync(nameof(GameHub.LeaveRoom), annaSession);
        var lobbiesBeforeBertil = annaLobbies;
        await bertil.InvokeAsync<HubResponse<PlayerSession>>(nameof(GameHub.JoinRoom), created.RoomCode, "Bertil");
        await hostSawBertil.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.That(annaLobbies, Is.EqualTo(lobbiesBeforeBertil));
    }

    [Test]
    public async Task RejoinRoom_AfterLostConnection_GivesBackTheSamePlace()
    {
        await using var host = CreateConnection();
        var annaAway = new TaskCompletionSource();
        var annaBack = new TaskCompletionSource();
        host.On<LobbyState>(nameof(IGameClient.LobbyUpdated), lobby =>
        {
            if (lobby.Players is [{ IsConnected: false }])
            {
                annaAway.TrySetResult();
            }
            else if (annaAway.Task.IsCompleted && lobby.Players is [{ IsConnected: true }])
            {
                annaBack.TrySetResult();
            }
        });
        await host.StartAsync();
        var created = (await host.InvokeAsync<HubResponse<HostSession>>(nameof(GameHub.CreateRoom), null)).Value!;

        PlayerSession annaSession;
        await using (var lostPhone = CreateConnection())
        {
            await lostPhone.StartAsync();
            annaSession = (await lostPhone.InvokeAsync<HubResponse<PlayerSession>>(nameof(GameHub.JoinRoom), created.RoomCode, "Anna")).Value!;
        }

        await annaAway.Task.WaitAsync(TimeSpan.FromSeconds(5));

        await using var phone = CreateConnection();
        await phone.StartAsync();
        var rejoined = await phone.InvokeAsync<HubResponse<PlayerSession>>(nameof(GameHub.RejoinRoom), annaSession);
        await annaBack.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.That(rejoined.Value, Is.EqualTo(annaSession));
    }

    [Test]
    public async Task HostLostConnection_PausesTheRoomForThePlayers()
    {
        await using var anna = CreateConnection();
        var paused = new TaskCompletionSource();
        anna.On<LobbyState>(nameof(IGameClient.LobbyUpdated), lobby =>
        {
            if (lobby.IsPaused)
            {
                paused.TrySetResult();
            }
        });
        await anna.StartAsync();

        await using (var host = CreateConnection())
        {
            await host.StartAsync();
            var created = (await host.InvokeAsync<HubResponse<HostSession>>(nameof(GameHub.CreateRoom), null)).Value!;
            await anna.InvokeAsync<HubResponse<PlayerSession>>(nameof(GameHub.JoinRoom), created.RoomCode, "Anna");
        }

        await paused.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task CreateRoom_WithPreviousRoom_TellsItsPlayersThatItClosed()
    {
        await using var host = CreateConnection();
        await using var anna = CreateConnection();
        var closed = new TaskCompletionSource();
        anna.On(nameof(IGameClient.RoomClosed), () => closed.TrySetResult());
        await Task.WhenAll(host.StartAsync(), anna.StartAsync());

        var old = (await host.InvokeAsync<HubResponse<HostSession>>(nameof(GameHub.CreateRoom), null)).Value!;
        await anna.InvokeAsync<HubResponse<PlayerSession>>(nameof(GameHub.JoinRoom), old.RoomCode, "Anna");
        await host.InvokeAsync<HubResponse<HostSession>>(nameof(GameHub.CreateRoom), old);

        await closed.Task.WaitAsync(TimeSpan.FromSeconds(5));
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
