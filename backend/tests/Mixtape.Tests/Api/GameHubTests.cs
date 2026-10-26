using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Mixtape.Api.Hubs;
using Mixtape.Application.Rooms;
using Mixtape.Domain.Rooms;

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
        var lobbies = new List<GameState>();
        var annaJoined = new TaskCompletionSource();
        host.On<GameState>(nameof(IGameClient.GameStateUpdated), state =>
        {
            lobbies.Add(state);
            if (state.Players.Count == 1)
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
        Assert.That(lobbies[^1].Players.Single(), Is.EqualTo(new GamePlayer(joined.Value.PlayerId, "Anna", 1, true, false, false)));
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
        bertil.On<GameState>(nameof(IGameClient.GameStateUpdated), state =>
        {
            if (state.Players.Count == 1 && state.Players[0].Name == "Bertil")
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
        host.On<GameState>(nameof(IGameClient.GameStateUpdated), state =>
        {
            if (state.Players.Any(player => player.Name == "Bertil"))
            {
                hostSawBertil.TrySetResult();
            }
        });
        anna.On<GameState>(nameof(IGameClient.GameStateUpdated), _ => annaLobbies++);
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
        host.On<GameState>(nameof(IGameClient.GameStateUpdated), state =>
        {
            if (state.Players is [{ IsConnected: false }])
            {
                annaAway.TrySetResult();
            }
            else if (annaAway.Task.IsCompleted && state.Players is [{ IsConnected: true }])
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
        anna.On<GameState>(nameof(IGameClient.GameStateUpdated), state =>
        {
            if (state.IsPaused)
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

    [Test]
    public async Task StartGame_SendsTheFirstRoundWithSettingsToTheWholeRoom()
    {
        await using var host = CreateConnection();
        await using var anna = CreateConnection();
        var started = new TaskCompletionSource<GameState>();
        anna.On<GameState>(nameof(IGameClient.GameStateUpdated), state =>
        {
            if (state.Phase == GamePhase.Submitting)
            {
                started.TrySetResult(state);
            }
        });
        await Task.WhenAll(host.StartAsync(), anna.StartAsync());

        var created = (await host.InvokeAsync<HubResponse<HostSession>>(nameof(GameHub.CreateRoom), null)).Value!;
        await anna.InvokeAsync<HubResponse<PlayerSession>>(nameof(GameHub.JoinRoom), created.RoomCode, "Anna");
        await host.InvokeAsync<HubResponse<PlayerSession>>(nameof(GameHub.JoinRoom), created.RoomCode, "Bertil");
        await host.InvokeAsync<HubResponse<PlayerSession>>(nameof(GameHub.JoinRoom), created.RoomCode, "Cecilia");
        var settings = await host.InvokeAsync<HubResponse<GameState>>(nameof(GameHub.UpdateSettings), created, 3, 10);
        var start = await host.InvokeAsync<HubResponse<GameState>>(nameof(GameHub.StartGame), created);
        var state = await started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.That(settings.Error, Is.Null);
        Assert.That(start.Error, Is.Null);
        Assert.That(state.Round, Is.EqualTo(1));
        Assert.That(state.Prompt, Is.Not.Null.And.Not.Empty);
        Assert.That(state.Settings, Is.EqualTo(new SettingsState(3, 10)));
        Assert.That(state.RemainingMs, Is.InRange(40_000, 45_000));
    }

    [Test]
    public async Task GameStateUpdated_SendsThePhaseAsCamelCaseText()
    {
        await using var host = CreateConnection();
        var raw = new TaskCompletionSource<JsonElement>();
        // JsonElement tar emot det som står på tråden, oberoende av klientens converter.
        host.On<JsonElement>(nameof(IGameClient.GameStateUpdated), state => raw.TrySetResult(state));
        await host.StartAsync();

        await host.InvokeAsync<HubResponse<HostSession>>(nameof(GameHub.CreateRoom), null);
        var state = await raw.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.That(state.GetProperty("phase").GetString(), Is.EqualTo("lobby"));
    }

    [Test]
    public async Task StartGame_WithTooFewPlayers_ReturnsTheErrorCode()
    {
        await using var host = CreateConnection();
        await host.StartAsync();

        var created = (await host.InvokeAsync<HubResponse<HostSession>>(nameof(GameHub.CreateRoom), null)).Value!;
        var start = await host.InvokeAsync<HubResponse<GameState>>(nameof(GameHub.StartGame), created);

        Assert.That(start.Error?.Code, Is.EqualTo("room.not_enough_players"));
    }

    // Testservern har ingen riktig port, så klienten går via dess HTTP-hanterare med long polling.
    // Klienten läser enums som text, som servern skickar dem.
    private HubConnection CreateConnection()
    {
        var server = _factory.Server;
        return new HubConnectionBuilder()
            .WithUrl(new Uri(server.BaseAddress, "gamehub"), HttpTransportType.LongPolling, options =>
            {
                options.HttpMessageHandlerFactory = _ => server.CreateHandler();
            })
            .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)))
            .Build();
    }
}
