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
using Mixtape.Domain.Rooms;

namespace Mixtape.Tests.Api;

// Låtarna genom rundan: sökningen, inskicket, uppspelningen och röstningen.
public sealed class TrackHubTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private FakeTimeProvider _time = null!;

    // Deezer byts mot en fast lista, så att testet aldrig går mot nätet. Klockan spolas fram av testet, men serverns tick går i realtid.
    [OneTimeSetUp]
    public void StartApi()
    {
        _time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 26, 20, 0, 0, TimeSpan.Zero));
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IMusicCatalog, FixedCatalog>();
                services.AddSingleton<TimeProvider>(_time);
            }));
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
        Assert.That(state.GetRawText(), Does.Not.Contain($"Låt {FixedCatalog.Tracks[0].Id}").And.Not.Contain(FixedCatalog.Tracks[0].Id.ToString()));
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

    // Under uppspelningen ser alla vilken låt som spelas, men varken vem som valde den eller länken till klippet.
    [Test]
    public async Task Playing_TellsTheRoomWhatIsPlayingWithoutWhoOrTheLink()
    {
        await using var host = CreateConnection();
        var players = new[] { CreateConnection(), CreateConnection(), CreateConnection() };
        var playing = new TaskCompletionSource<JsonElement>();
        players[0].On<JsonElement>(nameof(IGameClient.GameStateUpdated), state =>
        {
            if (state.GetProperty("phase").GetString() == "playing")
            {
                playing.TrySetResult(state);
            }
        });
        await Task.WhenAll(players.Select(player => player.StartAsync()).Append(host.StartAsync()));

        var (room, sessions) = await StartPlayingAsync(host, players);
        var state = await playing.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var clip = await host.InvokeAsync<HubResponse<Clip>>(nameof(GameHub.GetClip), room);
        // Spelaren har bara sin egen hemliga token, och den öppnar inte värdens anrop.
        var asPlayer = await players[0].InvokeAsync<HubResponse<Clip>>(nameof(GameHub.GetClip), room with { HostToken = sessions[0].PlayerToken });

        var nowPlaying = state.GetProperty("nowPlaying");
        Assert.That(nowPlaying.GetProperty("index").GetInt32(), Is.EqualTo(1));
        Assert.That(nowPlaying.GetProperty("count").GetInt32(), Is.EqualTo(3));
        Assert.That(nowPlaying.GetProperty("title").GetString(), Does.StartWith("Låt "));
        // Spelarnas id står med rätta i players, så id:t letas bara i nowPlaying. Länken får inte finnas någonstans i bilden.
        Assert.That(sessions, Has.None.Matches<PlayerSession>(session => nowPlaying.GetRawText().Contains(session.PlayerId)));
        Assert.That(state.GetRawText(), Does.Not.Contain("https://klipp/"));
        Assert.That(clip.Value!.PreviewUrl, Does.StartWith("https://klipp/"));
        Assert.That(asPlayer.Error?.Code, Is.EqualTo("room.not_found"));

        foreach (var player in players)
        {
            await player.DisposeAsync();
        }
    }

    // Under röstningen ser alla bidragen och vem som röstat, men varken vem som valde vad eller länkarna.
    [Test]
    public async Task Voting_TellsTheRoomTheBallotWithoutWhoOrTheLink()
    {
        await using var host = CreateConnection();
        var players = new[] { CreateConnection(), CreateConnection(), CreateConnection() };
        await Task.WhenAll(players.Select(player => player.StartAsync()).Append(host.StartAsync()));

        var (_, sessions) = await StartPlayingAsync(host, players);
        var state = await PlayAllClipsAsync(players[0]);

        var ballot = state.GetProperty("ballot");
        Assert.That(ballot.EnumerateArray().Select(entry => entry.GetProperty("number").GetInt32()), Is.EqualTo(new[] { 1, 2, 3 }));
        Assert.That(ballot.EnumerateArray().Select(entry => entry.GetProperty("title").GetString()), Is.EquivalentTo(new[] { "Låt 1", "Låt 2", "Låt 3" }));
        Assert.That(sessions, Has.None.Matches<PlayerSession>(session => ballot.GetRawText().Contains(session.PlayerId)));
        Assert.That(state.GetRawText(), Does.Not.Contain("https://klipp/"));
        Assert.That(state.GetProperty("players").EnumerateArray().Select(player => player.GetProperty("hasVoted").GetBoolean()), Has.All.False);

        foreach (var player in players)
        {
            await player.DisposeAsync();
        }
    }

    // Varje mobil får bara sin egen del: vilket bidrag som är dess eget och vad den röstat på. Storskärmen får ingenting.
    [Test]
    public async Task GetBallot_GivesEachPlayerTheirOwnEntryAndVote()
    {
        await using var host = CreateConnection();
        var players = new[] { CreateConnection(), CreateConnection(), CreateConnection() };
        await Task.WhenAll(players.Select(player => player.StartAsync()).Append(host.StartAsync()));

        await StartPlayingAsync(host, players);
        var state = await PlayAllClipsAsync(players[0]);
        var titles = state.GetProperty("ballot").EnumerateArray().ToDictionary(entry => entry.GetProperty("number").GetInt32(), entry => entry.GetProperty("title").GetString());

        var ballots = new List<Ballot>();
        foreach (var player in players)
        {
            ballots.Add((await player.InvokeAsync<HubResponse<Ballot>>(nameof(GameHub.GetBallot))).Value!);
        }

        // Spelare i skickade in låt i + 1, så det egna numret pekar på den låten.
        for (var i = 0; i < players.Length; i++)
        {
            Assert.That(titles[ballots[i].OwnEntry!.Value], Is.EqualTo($"Låt {i + 1}"));
            Assert.That(ballots[i].VotedEntry, Is.Null);
        }

        var own = await players[0].InvokeAsync<HubResponse<GameState>>(nameof(GameHub.Vote), ballots[0].OwnEntry!.Value);
        var other = ballots[1].OwnEntry!.Value;
        var voted = await players[0].InvokeAsync<HubResponse<GameState>>(nameof(GameHub.Vote), other);
        var after = (await players[0].InvokeAsync<HubResponse<Ballot>>(nameof(GameHub.GetBallot))).Value!;
        var asHost = await host.InvokeAsync<HubResponse<Ballot>>(nameof(GameHub.GetBallot));

        Assert.That(own.Error?.Code, Is.EqualTo("vote.own_entry"));
        Assert.That(voted.Value!.Players.Count(player => player.HasVoted), Is.EqualTo(1));
        Assert.That(after.VotedEntry, Is.EqualTo(other));
        Assert.That(asHost.Error?.Code, Is.EqualTo("room.not_found"));

        foreach (var player in players)
        {
            await player.DisposeAsync();
        }
    }

    // Först i avslöjandet får alla veta vem som valde vad, hur många röster låten fick och hur ställningen ser ut.
    [Test]
    public async Task Revealing_TellsTheRoomWhoPickedWhatAndTheStandings()
    {
        await using var host = CreateConnection();
        var players = new[] { CreateConnection(), CreateConnection(), CreateConnection() };
        await Task.WhenAll(players.Select(player => player.StartAsync()).Append(host.StartAsync()));

        var (_, sessions) = await StartPlayingAsync(host, players);
        var voting = await PlayAllClipsAsync(players[0]);
        var revealing = new TaskCompletionSource<JsonElement>();
        players[0].On<JsonElement>(nameof(IGameClient.GameStateUpdated), state =>
        {
            if (state.GetProperty("phase").GetString() == "revealing")
            {
                revealing.TrySetResult(state);
            }
        });

        // Varje spelare röstar på bidraget efter sitt eget, så att alla bidrag får en röst.
        foreach (var player in players)
        {
            var own = (await player.InvokeAsync<HubResponse<Ballot>>(nameof(GameHub.GetBallot))).Value!.OwnEntry!.Value;
            await player.InvokeAsync<HubResponse<GameState>>(nameof(GameHub.Vote), own % players.Length + 1);
        }

        var state = await revealing.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.That(voting.GetProperty("reveal").ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That(voting.GetProperty("standings").ValueKind, Is.EqualTo(JsonValueKind.Null));

        // Spelare i skickade in låt i + 1, så ägaren av varje bidrag går att kontrollera på titeln.
        var reveal = state.GetProperty("reveal").EnumerateArray().ToList();
        Assert.That(reveal.Select(entry => entry.GetProperty("number").GetInt32()), Is.EqualTo(new[] { 1, 2, 3 }));
        foreach (var entry in reveal)
        {
            var owner = int.Parse(entry.GetProperty("title").GetString()!["Låt ".Length..]) - 1;
            Assert.That(entry.GetProperty("playerId").GetString(), Is.EqualTo(sessions[owner].PlayerId));
            Assert.That(entry.GetProperty("votes").GetInt32(), Is.EqualTo(1));
        }

        var standings = state.GetProperty("standings").EnumerateArray().ToList();
        Assert.That(standings.Select(standing => standing.GetProperty("playerId").GetString()), Is.EquivalentTo(sessions.Select(session => session.PlayerId)));
        Assert.That(standings.Select(standing => (standing.GetProperty("score").GetInt32(), standing.GetProperty("roundPoints").GetInt32(), standing.GetProperty("place").GetInt32())), Has.All.EqualTo((1, 1, 1)));
        Assert.That(state.GetRawText(), Does.Not.Contain("https://klipp/"));

        foreach (var player in players)
        {
            await player.DisposeAsync();
        }
    }

    // Klockan spolas fram ett klipp i taget, eftersom servern byter klipp högst en gång per tick. Ger bilden när röstningen börjat.
    private async Task<JsonElement> PlayAllClipsAsync(HubConnection listener)
    {
        while (true)
        {
            var next = new TaskCompletionSource<JsonElement>();
            using var subscription = listener.On<JsonElement>(nameof(IGameClient.GameStateUpdated), state => next.TrySetResult(state));
            _time.Advance(TimeSpan.FromSeconds(GameSettings.Default.ClipSeconds));
            var state = await next.Task.WaitAsync(TimeSpan.FromSeconds(5));
            if (state.GetProperty("phase").GetString() == "voting")
            {
                return state;
            }
        }
    }

    // Tre spelare med var sin anslutning går med, och alla skickar in, så att rummet går direkt till uppspelningen. Ger spelarnas sessioner.
    private static async Task<(HostSession Room, List<PlayerSession> Sessions)> StartPlayingAsync(HubConnection host, HubConnection[] players)
    {
        var room = (await host.InvokeAsync<HubResponse<HostSession>>(nameof(GameHub.CreateRoom), null)).Value!;
        var sessions = new List<PlayerSession>();
        foreach (var (player, name) in players.Zip(["Anna", "Bertil", "Cecilia"]))
        {
            sessions.Add((await player.InvokeAsync<HubResponse<PlayerSession>>(nameof(GameHub.JoinRoom), room.RoomCode, name)).Value!);
        }

        await host.InvokeAsync<HubResponse<GameState>>(nameof(GameHub.StartGame), room);
        for (var i = 0; i < players.Length; i++)
        {
            await players[i].InvokeAsync<HubResponse<GameState>>(nameof(GameHub.SubmitTrack), (long)(i + 1));
        }

        return (room, sessions);
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
            Task.FromResult(Result<PlayableTrack>.Success(new PlayableTrack(trackId, $"Låt {trackId}", "Artist", "Album", "omslag", $"https://klipp/{trackId}")));
    }
}
