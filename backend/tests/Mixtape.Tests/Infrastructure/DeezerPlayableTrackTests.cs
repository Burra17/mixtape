using Microsoft.Extensions.Logging.Abstractions;
using Mixtape.Application.Music;
using Mixtape.Infrastructure.Music.Deezer;

namespace Mixtape.Tests.Infrastructure;

public sealed class DeezerPlayableTrackTests
{
    [Test]
    public async Task GetPlayableTrack_WithSavedResponse_ReturnsBigCoverAndFreshPreview()
    {
        var json = await File.ReadAllTextAsync(Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "deezer-track-44112901.json"));
        var (catalog, handler) = CreateCatalog(FakeHttpHandler.Json(json));

        var track = (await catalog.GetPlayableTrackAsync(44112901)).Value;

        Assert.That(track.Id, Is.EqualTo(44112901));
        Assert.That(track.Title, Is.EqualTo("Jag kommer"));
        Assert.That(track.Artist, Is.EqualTo("Veronica Maggio"));
        Assert.That(track.Album, Is.EqualTo("Satan i gatan (Bonus Version)"));
        Assert.That(track.CoverUrl, Does.EndWith("/1000x1000-000000-80-0-0.jpg"));
        Assert.That(track.PreviewUrl, Does.StartWith("https://cdnt-preview.dzcdn.net/").And.Contain(".mp3"));
        Assert.That(handler.Requests.Single().AbsoluteUri, Is.EqualTo("https://api.deezer.com/track/44112901"));
    }

    [Test]
    public async Task GetPlayableTrack_WithContributors_GivesAllOfThem()
    {
        var json = await File.ReadAllTextAsync(Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "deezer-track-4091932171.json"));
        var (catalog, _) = CreateCatalog(FakeHttpHandler.Json(json));

        var track = (await catalog.GetPlayableTrackAsync(4091932171)).Value;

        Assert.That(track.Title, Is.EqualTo("Under Pressure (feat. David Bowie)"));
        Assert.That(track.Artist, Is.EqualTo("Queen"));
        Assert.That(track.Contributors, Is.EqualTo(new[] { "Queen", "David Bowie" }));
        Assert.That(track.ToSubmittedTrack().Contributors, Is.EqualTo(new[] { "Queen", "David Bowie" }));
    }

    [Test]
    public async Task GetPlayableTrack_TrackThatDoesNotExist_ReturnsTrackUnavailable()
    {
        var (catalog, _) = CreateCatalog(FakeHttpHandler.Json("""{"error":{"type":"DataException","message":"no data","code":800}}"""));

        var result = await catalog.GetPlayableTrackAsync(1);

        Assert.That(result.Error, Is.EqualTo(MusicErrors.TrackUnavailable));
    }

    [TestCase("""{"id":2,"readable":false,"title":"Spärrad","preview":"https://p/a.mp3","artist":{"name":"A"},"album":{"title":"X","cover_xl":"xl"}}""")]
    [TestCase("""{"id":2,"readable":true,"title":"Utan klipp","preview":"","artist":{"name":"A"},"album":{"title":"X","cover_xl":"xl"}}""")]
    [TestCase("""{"id":2,"readable":true,"title":"Utan omslag","preview":"https://p/a.mp3","artist":{"name":"A"},"album":{"title":"X"}}""")]
    [TestCase("""{"id":2,"readable":true,"title":"Utan artistnamn","preview":"https://p/a.mp3","artist":{},"album":{"title":"X","cover_xl":"xl"}}""")]
    public async Task GetPlayableTrack_NotPlayable_ReturnsTrackUnavailable(string json)
    {
        var (catalog, _) = CreateCatalog(FakeHttpHandler.Json(json));

        var result = await catalog.GetPlayableTrackAsync(2);

        Assert.That(result.Error, Is.EqualTo(MusicErrors.TrackUnavailable));
    }

    [TestCase(4, "music.busy")]
    [TestCase(500, "music.unavailable")]
    public async Task GetPlayableTrack_OtherDeezerErrors_MapLikeTheSearch(int code, string expected)
    {
        var (catalog, _) = CreateCatalog(FakeHttpHandler.Json($$$"""{"error":{"type":"Exception","message":"fel","code":{{{code}}}}}"""));

        var result = await catalog.GetPlayableTrackAsync(2);

        Assert.That(result.Error?.Code, Is.EqualTo(expected));
    }

    [Test]
    public async Task GetPlayableTrack_WhenTheNetworkFails_ReturnsUnavailable()
    {
        var (catalog, _) = CreateCatalog(new FakeHttpHandler(_ => throw new HttpRequestException("nere")));

        var result = await catalog.GetPlayableTrackAsync(2);

        Assert.That(result.Error, Is.EqualTo(MusicErrors.Unavailable));
    }

    private static (DeezerMusicCatalog Catalog, FakeHttpHandler Handler) CreateCatalog(FakeHttpHandler handler)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.deezer.com/") };
        return (new DeezerMusicCatalog(http, NullLogger<DeezerMusicCatalog>.Instance), handler);
    }
}
