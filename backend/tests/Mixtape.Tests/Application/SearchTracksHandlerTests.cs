using Mixtape.Application.Music;
using Mixtape.Application.Rooms;
using Mixtape.Domain.Common;
using Mixtape.Domain.Rooms;
using Mixtape.Infrastructure.Connections;
using Mixtape.Infrastructure.Rooms;

namespace Mixtape.Tests.Application;

public sealed class SearchTracksHandlerTests
{
    private FakeTimeProvider _time = null!;
    private InMemoryGameRoomRepository _rooms = null!;
    private InMemoryConnectionRegistry _connections = null!;
    private RecordingCatalog _catalog = null!;
    private SearchTracksHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 22, 20, 0, 0, TimeSpan.Zero));
        _rooms = new InMemoryGameRoomRepository();
        _connections = new InMemoryConnectionRegistry();
        _catalog = new RecordingCatalog();
        _handler = new SearchTracksHandler(_catalog, _connections, _rooms);
    }

    [Test]
    public async Task Handle_AsPlayer_SearchesWithTheNormalizedQuery()
    {
        await GameStarter.StartAsync(_rooms, _connections, _time);

        var result = await _handler.HandleAsync("Anna", "  Veronica   Maggio ");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(_catalog.Queries, Is.EqualTo(new[] { "veronica maggio" }));
    }

    [TestCase("host")]
    [TestCase("okänd")]
    public async Task Handle_AsHostOrUnknownConnection_ReturnsRoomNotFound(string connectionId)
    {
        await GameStarter.StartAsync(_rooms, _connections, _time);

        var result = await _handler.HandleAsync(connectionId, "kent");

        Assert.That(result.Error, Is.EqualTo(RoomErrors.NotFound));
        Assert.That(_catalog.Queries, Is.Empty);
    }

    [TestCase(null)]
    [TestCase("   ")]
    public async Task Handle_WithEmptyQuery_ReturnsInvalidQueryWithoutSearching(string? query)
    {
        await GameStarter.StartAsync(_rooms, _connections, _time);

        var result = await _handler.HandleAsync("Anna", query);

        Assert.That(result.Error, Is.EqualTo(MusicErrors.InvalidQuery));
        Assert.That(_catalog.Queries, Is.Empty);
    }

    [Test]
    public async Task Handle_InTheLobby_ReturnsWrongPhaseWithoutSearching()
    {
        var host = (await new CreateRoomHandler(_rooms, _connections, _time).HandleAsync(null, "host")).Value.Session;
        await new JoinRoomHandler(_rooms, _connections, _time).HandleAsync(host.RoomCode, "Anna", "Anna");

        var result = await _handler.HandleAsync("Anna", "kent");

        Assert.That(result.Error, Is.EqualTo(RoomErrors.WrongPhase));
        Assert.That(_catalog.Queries, Is.Empty);
    }

    private sealed class RecordingCatalog : MusicCatalogStub
    {
        public List<string> Queries { get; } = [];

        public override Task<Result<IReadOnlyList<Track>>> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default)
        {
            Queries.Add(query.Value);
            return Task.FromResult(Result<IReadOnlyList<Track>>.Success([]));
        }

        public override Task<Result<PlayableTrack>> GetPlayableTrackAsync(long trackId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Sökningen hämtar inga klipp.");
    }
}
