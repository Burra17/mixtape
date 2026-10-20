using Microsoft.Extensions.Caching.Memory;
using Mixtape.Application.Music;
using Mixtape.Domain.Common;
using Mixtape.Infrastructure.Music;

namespace Mixtape.Tests.Infrastructure;

public sealed class CachingMusicCatalogTests
{
    private CountingCatalog _inner = null!;
    private CachingMusicCatalog _catalog = null!;

    [SetUp]
    public void SetUp()
    {
        _inner = new CountingCatalog();
        _catalog = new CachingMusicCatalog(_inner, new MemoryCache(new MemoryCacheOptions()));
    }

    [Test]
    public async Task Search_SameQueryTwice_GoesToTheSourceOnce()
    {
        var first = await _catalog.SearchAsync(Query("Veronica Maggio"));
        var second = await _catalog.SearchAsync(Query("  veronica   MAGGIO "));

        Assert.That(_inner.Calls, Is.EqualTo(1));
        Assert.That(second.Value, Is.SameAs(first.Value));
    }

    [Test]
    public async Task Search_DifferentQueries_GoToTheSourceEachTime()
    {
        await _catalog.SearchAsync(Query("kent"));
        await _catalog.SearchAsync(Query("robyn"));

        Assert.That(_inner.Calls, Is.EqualTo(2));
    }

    [Test]
    public async Task Search_AfterAnError_GoesToTheSourceAgain()
    {
        _inner.Next = MusicErrors.Unavailable;
        await _catalog.SearchAsync(Query("kent"));
        _inner.Next = null;

        var retry = await _catalog.SearchAsync(Query("kent"));

        Assert.That(_inner.Calls, Is.EqualTo(2));
        Assert.That(retry.IsSuccess, Is.True);
    }

    [Test]
    public async Task Search_WithNoHits_CachesTheEmptyList()
    {
        _inner.Tracks = [];
        await _catalog.SearchAsync(Query("xqzjwvkk"));
        await _catalog.SearchAsync(Query("xqzjwvkk"));

        Assert.That(_inner.Calls, Is.EqualTo(1));
    }

    [Test]
    public async Task GetPlayableTrack_SameTrackTwice_GoesToTheSourceEachTime()
    {
        var first = await _catalog.GetPlayableTrackAsync(44112901);
        var second = await _catalog.GetPlayableTrackAsync(44112901);

        Assert.That(_inner.Calls, Is.EqualTo(2));
        Assert.That(second.Value.PreviewUrl, Is.Not.EqualTo(first.Value.PreviewUrl));
    }

    private static SearchQuery Query(string text) => SearchQuery.Create(text).Value;

    private sealed class CountingCatalog : IMusicCatalog
    {
        public int Calls { get; private set; }

        public Error? Next { get; set; }

        public IReadOnlyList<Track> Tracks { get; set; } = [new Track(1, "Jag kommer", "Veronica Maggio", "Satan i gatan", "c", false)];

        public Task<Result<IReadOnlyList<Track>>> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(Next is { } error ? Result<IReadOnlyList<Track>>.Failure(error) : Result<IReadOnlyList<Track>>.Success(Tracks));
        }

        public Task<Result<PlayableTrack>> GetPlayableTrackAsync(long trackId, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(Result<PlayableTrack>.Success(new PlayableTrack(trackId, "Jag kommer", "Veronica Maggio", "Satan i gatan", "xl", $"klipp-{Calls}")));
        }
    }
}
