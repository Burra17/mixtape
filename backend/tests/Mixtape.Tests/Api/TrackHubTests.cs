using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Mixtape.Api.Hubs;
using Mixtape.Application.Music;
using Mixtape.Application.Rooms;
using Mixtape.Domain.Common;

namespace Mixtape.Tests.Api;

// Sökningen och inskicket, de två anropen som går till musikkällan.
public sealed class TrackHubTests
{
    private WebApplicationFactory<Program> _factory = null!;

    // Deezer byts mot en fast lista, så att testet aldrig går mot nätet.
    [OneTimeSetUp]
    public void StartApi()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddSingleton<IMusicCatalog, FixedCatalog>()));
    }

    [OneTimeTearDown]
    public void StopApi()
    {
        _factory.Dispose();
    }

    [Test]
    public async Task Search_AsPlayerDuringSubmitting_ReturnsTracks()
    {
        await using var host = CreateConnection();
        await using var anna = CreateConnection();
        await Task.WhenAll(host.StartAsync(), anna.StartAsync());
        await StartGameAsync(host, anna);

        var result = await anna.InvokeAsync<HubResponse<IReadOnlyList<Track>>>(nameof(GameHub.Search), "Veronica Maggio");

        Assert.That(result.Value, Is.EqualTo(FixedCatalog.Tracks));
    }

    [Test]
    public async Task Search_InTheLobby_ReturnsWrongPhase()
    {
        await using var host = CreateConnection();
        await using var anna = CreateConnection();
        await Task.WhenAll(host.StartAsync(), anna.StartAsync());
        var room = (await host.InvokeAsync<HubResponse<HostSession>>(nameof(GameHub.CreateRoom), null)).Value!;
        await anna.InvokeAsync<HubResponse<PlayerSession>>(nameof(GameHub.JoinRoom), room.RoomCode, "Anna");

        var result = await anna.InvokeAsync<HubResponse<IReadOnlyList<Track>>>(nameof(GameHub.Search), "kent");

        Assert.That(result.Error?.Code, Is.EqualTo("room.wrong_phase"));
    }

    [Test]
    public async Task Search_AsHostOrOutsideARoom_ReturnsRoomNotFound()
    {
        await using var host = CreateConnection();
        await using var stranger = CreateConnection();
        await Task.WhenAll(host.StartAsync(), stranger.StartAsync());
        await host.InvokeAsync<HubResponse<HostSession>>(nameof(GameHub.CreateRoom), null);

        var asHost = await host.InvokeAsync<HubResponse<IReadOnlyList<Track>>>(nameof(GameHub.Search), "kent");
        var asStranger = await stranger.InvokeAsync<HubResponse<IReadOnlyList<Track>>>(nameof(GameHub.Search), "kent");

        Assert.That(asHost.Error?.Code, Is.EqualTo("room.not_found"));
        Assert.That(asStranger.Error?.Code, Is.EqualTo("room.not_found"));
    }

    // Storskärmen får veta vem som är klar, men ingenting i bilden avslöjar vilken låt det var.
    [Test]
    public async Task SubmitTrack_TellsTheRoomWhoIsDoneWithoutTheTrack()
    {
        await using var host = CreateConnection();
        await using var anna = CreateConnection();
        var submitted = new TaskCompletionSource<JsonElement>();
        host.On<JsonElement>(nameof(IGameClient.GameStateUpdated), state =>
        {
            if (state.GetProperty("players").EnumerateArray().Any(player => player.GetProperty("hasSubmitted").GetBoolean()))
            {
                submitted.TrySetResult(state);
            }
        });
        await Task.WhenAll(host.StartAsync(), anna.StartAsync());
        var annaId = await StartGameAsync(host, anna);

        var result = await anna.InvokeAsync<HubResponse<GameState>>(nameof(GameHub.SubmitTrack), FixedCatalog.Tracks[0].Id);
        var state = await submitted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.That(result.Error, Is.Null);
        var done = state.GetProperty("players").EnumerateArray().Single(player => player.GetProperty("hasSubmitted").GetBoolean());
        Assert.That(done.GetProperty("id").GetString(), Is.EqualTo(annaId));
        Assert.That(state.GetRawText(), Does.Not.Contain("Jag kommer").And.Not.Contain(FixedCatalog.Tracks[0].Id.ToString()));
    }

    [Test]
    public async Task SubmitTrack_WithATakenTrack_ReturnsTheErrorCode()
    {
        await using var host = CreateConnection();
        await using var anna = CreateConnection();
        await Task.WhenAll(host.StartAsync(), anna.StartAsync());
        await StartGameAsync(host, anna);
        await host.InvokeAsync<HubResponse<GameState>>(nameof(GameHub.SubmitTrack), FixedCatalog.Tracks[0].Id);

        var result = await anna.InvokeAsync<HubResponse<GameState>>(nameof(GameHub.SubmitTrack), FixedCatalog.Tracks[0].Id);

        Assert.That(result.Error?.Code, Is.EqualTo("entry.track_taken"));
    }

    // Anna har en egen anslutning. Storskärmens anslutning går med som Bertil och sedan Cecilia, så att spelet kan starta,
    // och anropar därefter som Cecilia. Ger Annas id.
    private static async Task<string> StartGameAsync(HubConnection host, HubConnection anna)
    {
        var room = (await host.InvokeAsync<HubResponse<HostSession>>(nameof(GameHub.CreateRoom), null)).Value!;
        var joined = (await anna.InvokeAsync<HubResponse<PlayerSession>>(nameof(GameHub.JoinRoom), room.RoomCode, "Anna")).Value!;
        await host.InvokeAsync<HubResponse<PlayerSession>>(nameof(GameHub.JoinRoom), room.RoomCode, "Bertil");
        await host.InvokeAsync<HubResponse<PlayerSession>>(nameof(GameHub.JoinRoom), room.RoomCode, "Cecilia");
        await host.InvokeAsync<HubResponse<GameState>>(nameof(GameHub.StartGame), room);
        return joined.PlayerId;
    }

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

    private sealed class FixedCatalog : IMusicCatalog
    {
        public static readonly IReadOnlyList<Track> Tracks = [new Track(44112901, "Jag kommer", "Veronica Maggio", "Satan i gatan", "c", false)];

        public Task<Result<IReadOnlyList<Track>>> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<IReadOnlyList<Track>>.Success(Tracks));

        public Task<Result<PlayableTrack>> GetPlayableTrackAsync(long trackId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<PlayableTrack>.Success(new PlayableTrack(trackId, "Jag kommer", "Veronica Maggio", "Satan i gatan", "c", "klipp")));
    }
}
