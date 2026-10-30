using System.Text.Json;
using Mixtape.Application.Music;
using Mixtape.Application.Rooms;
using Mixtape.Domain.Common;
using Mixtape.Domain.Games;
using Mixtape.Domain.Games.DropTheNeedle;
using Mixtape.Infrastructure.Categories;
using Mixtape.Infrastructure.Connections;
using Mixtape.Infrastructure.Rooms;

namespace Mixtape.Tests.Application;

// Starten, gissningen och klippen i Drop the Needle, med en påhittad musikkälla i stället för Deezer.
public sealed class DropTheNeedleHandlersTests
{
    private static readonly CategoryCatalog Categories = new([new Category("00s", "00-tal", [1]), new Category("rock", "Rock", [2])]);

    private FakeTimeProvider _time = null!;
    private InMemoryGameRoomRepository _rooms = null!;
    private InMemoryConnectionRegistry _connections = null!;
    private SongCatalog _catalog = null!;
    private RecordingNotifier _notifier = null!;
    private HostSession _host = null!;

    [SetUp]
    public async Task SetUp()
    {
        _time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 30, 20, 0, 0, TimeSpan.Zero));
        _rooms = new InMemoryGameRoomRepository();
        _connections = new InMemoryConnectionRegistry();
        _catalog = new SongCatalog();
        _notifier = new RecordingNotifier();
        _host = (await new CreateRoomHandler(_rooms, _connections, _time).HandleAsync(null, "host")).Value.Session;
        var join = new JoinRoomHandler(_rooms, _connections, _time);
        await join.HandleAsync(_host.RoomCode, "Anna", "Anna");
        await join.HandleAsync(_host.RoomCode, "Bertil", "Bertil");
        await new ChooseModeHandler(_rooms, _time).HandleAsync(_host, GameMode.DropTheNeedle);
    }

    [Test]
    public async Task Start_DrawsTheSongsAndBeginsGuessingWithoutTheAnswerInTheState()
    {
        var state = (await StartAsync()).Value;

        Assert.That(state.Phase, Is.EqualTo(GamePhase.Guessing));
        Assert.That(state.Song, Is.EqualTo(new SongState(1, 10, "00s", 1)));
        Assert.That(state.NowPlaying, Is.Null);
        Assert.That(JsonSerializer.Serialize(state), Does.Not.Contain("Hemlig"));
    }

    [Test]
    public async Task Start_WithFewerSongsThanTheGame_ReturnsTooFewSongs()
    {
        _catalog.PlaylistSize = 5;

        var result = await StartAsync();

        Assert.That(result.Error, Is.EqualTo(DropTheNeedleErrors.TooFewSongs));
        Assert.That(await PhaseAsync(), Is.EqualTo(GamePhase.Lobby));
    }

    [Test]
    public async Task Start_WhenDeezerFails_StaysInTheLobby()
    {
        _catalog.PlaylistError = MusicErrors.Unavailable;

        var result = await StartAsync();

        Assert.That(result.Error, Is.EqualTo(MusicErrors.SongsUnavailable));
        Assert.That(await PhaseAsync(), Is.EqualTo(GamePhase.Lobby));
    }

    [Test]
    public async Task Start_WhenTheCategoryChangesWhileLoading_ReturnsSettingsChanged()
    {
        _catalog.WhileLoading = () => new UpdateDropTheNeedleSettingsHandler(_rooms, Categories, _time).HandleAsync(_host, null, null, "rock").Wait();

        var result = await StartAsync();

        Assert.That(result.Error, Is.EqualTo(GameModeErrors.SettingsChanged));
        Assert.That(await PhaseAsync(), Is.EqualTo(GamePhase.Lobby));
    }

    [Test]
    public async Task Guess_TakesTheTimeWhenItArrivesNotWhenDeezerAnswers()
    {
        await StartAsync();
        var arrived = _time.GetUtcNow();
        _catalog.WhileFetchingTrack = () => _time.Advance(TimeSpan.FromSeconds(2));

        var state = (await new GuessHandler(_rooms, _catalog, _connections, _time).HandleAsync("Anna", 500)).Value;

        var guess = await _rooms.WithRoomAsync(_host.RoomCode, room => Result<Guess>.Success(((DropTheNeedleGame)room.Game!).GuessOf(state.Players[0].Id)!));
        Assert.That(guess.Value.ReceivedAt, Is.EqualTo(arrived));
        Assert.That(state.Players[0].HasGuessed, Is.True);
        Assert.That(state.Players[1].HasGuessed, Is.False);
    }

    [Test]
    public async Task TheReveal_HasTheAnswerAndEveryonesGuessAndKeepsTheClip()
    {
        await StartAsync();
        await new GuessHandler(_rooms, _catalog, _connections, _time).HandleAsync("Anna", 500);
        _time.Advance(TimeSpan.FromSeconds(DropTheNeedleSettings.Default.ClipSeconds));

        var state = (await _rooms.WithRoomAsync(_host.RoomCode, room =>
        {
            room.Tick(_time.GetUtcNow());
            return Result<GameState>.Success(GameState.From(room, _time.GetUtcNow()));
        })).Value;

        Assert.That(state.Phase, Is.EqualTo(GamePhase.SongReveal));
        Assert.That(state.Song, Is.EqualTo(new SongState(1, 10, "00s", 1)));
        Assert.That(state.Answer!.Song.Title, Does.StartWith("Hemlig"));
        Assert.That(state.Answer.Results[0].Guess, Is.EqualTo(new RevealedSong("Låt 500", "Artist", "Album", "omslag")));
        Assert.That(state.Answer.Results[1].Guess, Is.Null);
        Assert.That(state.Standings, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task Guess_InTheLobby_ReturnsWrongPhaseWithoutAskingDeezer()
    {
        var result = await new GuessHandler(_rooms, _catalog, _connections, _time).HandleAsync("Anna", 500);

        Assert.That(result.Error?.Code, Is.EqualTo("room.wrong_phase"));
        Assert.That(_catalog.TrackFetches, Is.Zero);
    }

    [Test]
    public async Task GetClip_ForTheFirstSong_RenewsTheLinkAndKeepsTheContributors()
    {
        await StartAsync();

        var clip = (await new GetClipHandler(_rooms, _catalog, _notifier, _time).HandleAsync(_host)).Value;

        Assert.That(clip.Index, Is.EqualTo(1));
        Assert.That(clip.PreviewUrl, Does.StartWith("https://ny/"));
        Assert.That(_catalog.TrackFetches, Is.EqualTo(1));
        var current = await _rooms.WithRoomAsync(_host.RoomCode, room => ((DropTheNeedleGame)room.Game!).GetCurrentClip());
        Assert.That(current.Value.Track.Contributors, Is.EqualTo(new[] { "Artist", "Gäst" }));
    }

    [Test]
    public async Task GetClip_ForASongThatCannotBePlayed_SkipsItAndSendsTheState()
    {
        await StartAsync();
        _catalog.Unplayable = true;

        var result = await new GetClipHandler(_rooms, _catalog, _notifier, _time).HandleAsync(_host);

        Assert.That(result.Error, Is.EqualTo(MusicErrors.TrackUnavailable));
        Assert.That(_notifier.Sent.Single().Song, Is.EqualTo(new SongState(1, 9, "00s", 2)));
    }

    private Task<Result<GameState>> StartAsync() =>
        new StartGameHandler(_rooms, new NoPrompts(), new CategorySongs(Categories, _catalog), _time).HandleAsync(_host);

    private async Task<GamePhase> PhaseAsync() =>
        (await _rooms.WithRoomAsync(_host.RoomCode, room => Result<GameState>.Success(GameState.From(room, _time.GetUtcNow())))).Value.Phase;

    private sealed class NoPrompts : IPromptProvider
    {
        public IReadOnlyList<string> Draw(int count, IReadOnlyCollection<string> used) => throw new NotSupportedException();
    }

    // Spellistans låtar heter "Hemlig 1" och så vidare, så att testet ser om svaret läckt ut i bilden.
    private sealed class SongCatalog : MusicCatalogStub
    {
        public int PlaylistSize { get; set; } = 30;

        public Error? PlaylistError { get; set; }

        public bool Unplayable { get; set; }

        public Action? WhileLoading { get; set; }

        public Action? WhileFetchingTrack { get; set; }

        public int TrackFetches { get; private set; }

        public override Task<Result<IReadOnlyList<PlayableTrack>>> GetPlaylistTracksAsync(long playlistId, CancellationToken cancellationToken = default)
        {
            WhileLoading?.Invoke();
            IReadOnlyList<PlayableTrack> tracks = Enumerable.Range(1, PlaylistSize)
                .Select(id => new PlayableTrack(id, $"Hemlig {id}", $"Artist {id}", "Album", "omslag", $"https://gammal/{id}"))
                .ToList();
            return Task.FromResult(PlaylistError is { } error ? Result<IReadOnlyList<PlayableTrack>>.Failure(error) : Result<IReadOnlyList<PlayableTrack>>.Success(tracks));
        }

        public override Task<Result<PlayableTrack>> GetPlayableTrackAsync(long trackId, CancellationToken cancellationToken = default)
        {
            TrackFetches++;
            WhileFetchingTrack?.Invoke();
            return Task.FromResult(Unplayable
                ? Result<PlayableTrack>.Failure(MusicErrors.TrackUnavailable)
                : Result<PlayableTrack>.Success(new PlayableTrack(trackId, $"Låt {trackId}", "Artist", "Album", "omslag", $"https://ny/{trackId}", ["Artist", "Gäst"])));
        }
    }
}
