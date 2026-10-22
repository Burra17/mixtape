using Mixtape.Application.Music;
using Mixtape.Application.Rooms;
using Mixtape.Domain.Common;
using Mixtape.Domain.Rooms;
using Mixtape.Infrastructure.Connections;
using Mixtape.Infrastructure.Rooms;

namespace Mixtape.Tests.Application;

public sealed class GetClipHandlerTests
{
    private FakeTimeProvider _time = null!;
    private InMemoryGameRoomRepository _rooms = null!;
    private InMemoryConnectionRegistry _connections = null!;
    private CountingCatalog _catalog = null!;
    private GetClipHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 22, 20, 0, 0, TimeSpan.Zero));
        _rooms = new InMemoryGameRoomRepository();
        _connections = new InMemoryConnectionRegistry();
        _catalog = new CountingCatalog();
        _handler = new GetClipHandler(_rooms, _catalog, _time);
    }

    [Test]
    public async Task Handle_WithAFreshLink_ReturnsTheStoredLinkWithoutDeezer()
    {
        var game = await StartPlayingAsync();

        var clip = (await _handler.HandleAsync(game.Host)).Value;

        Assert.That(clip.Index, Is.EqualTo(1));
        Assert.That(clip.PreviewUrl, Does.StartWith("https://klipp/"));
        Assert.That(_catalog.Lookups, Is.Zero);
    }

    // Storskärmen var borta så länge att länken hann bli gammal, men klippet står still under pausen.
    [Test]
    public async Task Handle_AfterALongPause_FetchesANewLink()
    {
        var game = await StartPlayingAsync();
        await _rooms.WithRoomAsync(game.Host.RoomCode, room =>
        {
            room.DisconnectHost(_time.GetUtcNow());
            return Result.Success();
        });
        _time.Advance(Entry.LinkMaxAge);
        await new ResumeRoomHandler(_rooms, _connections, _time).HandleAsync(game.Host, "host");

        var clip = (await _handler.HandleAsync(game.Host)).Value;
        var again = (await _handler.HandleAsync(game.Host)).Value;

        Assert.That(clip.PreviewUrl, Does.StartWith("https://ny/"));
        Assert.That(again.PreviewUrl, Is.EqualTo(clip.PreviewUrl));
        Assert.That(_catalog.Lookups, Is.EqualTo(1));
    }

    [Test]
    public async Task Handle_WithWrongHostToken_ReturnsRoomNotFound()
    {
        var game = await StartPlayingAsync();

        var result = await _handler.HandleAsync(game.Host with { HostToken = "fel" });

        Assert.That(result.Error, Is.EqualTo(RoomErrors.NotFound));
    }

    [Test]
    public async Task Handle_DuringSubmitting_ReturnsWrongPhase()
    {
        var game = await GameStarter.StartAsync(_rooms, _connections, _time);

        var result = await _handler.HandleAsync(game.Host);

        Assert.That(result.Error, Is.EqualTo(RoomErrors.WrongPhase));
    }

    // Alla tre skickar in, så att rummet går direkt till uppspelningen.
    private async Task<StartedGame> StartPlayingAsync()
    {
        var game = await GameStarter.StartAsync(_rooms, _connections, _time);
        await _rooms.WithRoomAsync(game.Host.RoomCode, room =>
        {
            for (var i = 0; i < game.Players.Count; i++)
            {
                room.SubmitTrack(game.Players[i].PlayerId, TestTrack.Of(i + 1, $"Låt {i + 1}"), _time.GetUtcNow());
            }

            return Result.Success();
        });
        return game;
    }

    private sealed class CountingCatalog : IMusicCatalog
    {
        public int Lookups { get; private set; }

        public Task<Result<IReadOnlyList<Track>>> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Uppspelningen söker inte.");

        public Task<Result<PlayableTrack>> GetPlayableTrackAsync(long trackId, CancellationToken cancellationToken = default)
        {
            Lookups++;
            return Task.FromResult(Result<PlayableTrack>.Success(new PlayableTrack(trackId, $"Låt {trackId}", "Artist", "Album", "omslag", $"https://ny/{trackId}")));
        }
    }
}
