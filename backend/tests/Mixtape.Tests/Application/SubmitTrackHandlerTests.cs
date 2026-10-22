using Mixtape.Application.Music;
using Mixtape.Application.Rooms;
using Mixtape.Domain.Common;
using Mixtape.Domain.Rooms;
using Mixtape.Infrastructure.Connections;
using Mixtape.Infrastructure.Rooms;

namespace Mixtape.Tests.Application;

public sealed class SubmitTrackHandlerTests
{
    private FakeTimeProvider _time = null!;
    private InMemoryGameRoomRepository _rooms = null!;
    private InMemoryConnectionRegistry _connections = null!;
    private FakeCatalog _catalog = null!;
    private SubmitTrackHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 22, 20, 0, 0, TimeSpan.Zero));
        _rooms = new InMemoryGameRoomRepository();
        _connections = new InMemoryConnectionRegistry();
        _catalog = new FakeCatalog();
        _handler = new SubmitTrackHandler(_rooms, _catalog, _connections, _time);
    }

    [Test]
    public async Task Handle_AsPlayer_SubmitsTheTrackFromTheCatalog()
    {
        var game = await GameStarter.StartAsync(_rooms, _connections, _time);

        var result = await _handler.HandleAsync("Anna", 44112901);

        Assert.That(result.Value.Players.Single(player => player.Id == game.Players[0].PlayerId).HasSubmitted, Is.True);
        Assert.That(await EntriesAsync(game), Is.EqualTo(new[] { new Entry(game.Players[0].PlayerId, new SubmittedTrack(44112901, "Låt 44112901", "Artist")) }));
    }

    [Test]
    public async Task Handle_WhenTheTrackCannotBePlayed_ReturnsTheErrorWithoutAnEntry()
    {
        var game = await GameStarter.StartAsync(_rooms, _connections, _time);
        _catalog.Unavailable = true;

        var result = await _handler.HandleAsync("Anna", 44112901);

        Assert.That(result.Error, Is.EqualTo(MusicErrors.TrackUnavailable));
        Assert.That(await EntriesAsync(game), Is.Empty);
    }

    [TestCase("host")]
    [TestCase("okänd")]
    public async Task Handle_AsHostOrUnknownConnection_ReturnsRoomNotFoundWithoutLookingUpTheTrack(string connectionId)
    {
        await GameStarter.StartAsync(_rooms, _connections, _time);

        var result = await _handler.HandleAsync(connectionId, 44112901);

        Assert.That(result.Error, Is.EqualTo(RoomErrors.NotFound));
        Assert.That(_catalog.Lookups, Is.Zero);
    }

    private async Task<IReadOnlyList<Entry>> EntriesAsync(StartedGame game) =>
        (await _rooms.WithRoomAsync<IReadOnlyList<Entry>>(game.Host.RoomCode, room => room.Entries.ToList())).Value;

    private sealed class FakeCatalog : IMusicCatalog
    {
        public bool Unavailable { get; set; }

        public int Lookups { get; private set; }

        public Task<Result<IReadOnlyList<Track>>> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Inskicket söker inte.");

        public Task<Result<PlayableTrack>> GetPlayableTrackAsync(long trackId, CancellationToken cancellationToken = default)
        {
            Lookups++;
            return Task.FromResult(Unavailable
                ? Result<PlayableTrack>.Failure(MusicErrors.TrackUnavailable)
                : Result<PlayableTrack>.Success(new PlayableTrack(trackId, $"Låt {trackId}", "Artist", "Album", "omslag", "klipp")));
        }
    }
}
