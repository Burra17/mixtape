using Mixtape.Application.Connections;
using Mixtape.Application.Music;
using Mixtape.Domain.Common;
using Mixtape.Domain.Rooms;
using Mixtape.Infrastructure.Connections;

namespace Mixtape.Tests.Application;

public sealed class SearchTracksHandlerTests
{
    private InMemoryConnectionRegistry _connections = null!;
    private RecordingCatalog _catalog = null!;
    private SearchTracksHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _connections = new InMemoryConnectionRegistry();
        _catalog = new RecordingCatalog();
        _handler = new SearchTracksHandler(_catalog, _connections);
    }

    [Test]
    public async Task Handle_AsPlayer_SearchesWithTheNormalizedQuery()
    {
        _connections.Register("anna", ConnectionOwner.Player("BCDF", "id"));

        var result = await _handler.HandleAsync("anna", "  Veronica   Maggio ");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(_catalog.Queries, Is.EqualTo(new[] { "veronica maggio" }));
    }

    [TestCase("host")]
    [TestCase("okänd")]
    public async Task Handle_AsHostOrUnknownConnection_ReturnsRoomNotFound(string connectionId)
    {
        _connections.Register("host", ConnectionOwner.Host("BCDF"));

        var result = await _handler.HandleAsync(connectionId, "kent");

        Assert.That(result.Error, Is.EqualTo(RoomErrors.NotFound));
        Assert.That(_catalog.Queries, Is.Empty);
    }

    [TestCase(null)]
    [TestCase("   ")]
    public async Task Handle_WithEmptyQuery_ReturnsInvalidQueryWithoutSearching(string? query)
    {
        _connections.Register("anna", ConnectionOwner.Player("BCDF", "id"));

        var result = await _handler.HandleAsync("anna", query);

        Assert.That(result.Error, Is.EqualTo(MusicErrors.InvalidQuery));
        Assert.That(_catalog.Queries, Is.Empty);
    }

    private sealed class RecordingCatalog : IMusicCatalog
    {
        public List<string> Queries { get; } = [];

        public Task<Result<IReadOnlyList<Track>>> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default)
        {
            Queries.Add(query.Value);
            return Task.FromResult(Result<IReadOnlyList<Track>>.Success([]));
        }

        public Task<Result<PlayableTrack>> GetPlayableTrackAsync(long trackId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Sökningen hämtar inga klipp.");
    }
}
