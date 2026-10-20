using Microsoft.Extensions.Logging.Abstractions;
using Mixtape.Application.Music;
using Mixtape.Infrastructure.Music.Deezer;

namespace Mixtape.Tests.Infrastructure;

public sealed class DeezerMusicCatalogTests
{
    private static readonly SearchQuery VeronicaMaggio = SearchQuery.Create("Veronica Maggio").Value;

    [Test]
    public async Task Search_MapsTheSavedResponseToEightTracks()
    {
        var json = await File.ReadAllTextAsync(Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "deezer-search-veronica-maggio.json"));
        var (catalog, handler) = CreateCatalog(FakeHttpHandler.Json(json));

        var tracks = (await catalog.SearchAsync(VeronicaMaggio)).Value;

        Assert.That(tracks, Has.Count.EqualTo(IMusicCatalog.MaxResults));
        Assert.That(tracks[0], Is.EqualTo(new Track(
            44112901,
            "Jag kommer",
            "Veronica Maggio",
            "Satan i gatan (Bonus Version)",
            "https://cdn-images.dzcdn.net/images/cover/06333028fa315552903b68adfffcc0b3/250x250-000000-80-0-0.jpg",
            false)));
        Assert.That(handler.Requests.Single().AbsoluteUri, Is.EqualTo("https://api.deezer.com/search?q=veronica%20maggio&limit=25"));
    }

    [Test]
    public async Task Search_LeavesOutTracksWithoutPreviewOrNotReadable()
    {
        var json = """
            {"data":[
              {"id":1,"readable":true,"title":"Med klipp","preview":"https://cdnt-preview.dzcdn.net/a.mp3","explicit_lyrics":true,"artist":{"name":"A"},"album":{"title":"X","cover_medium":"c1"}},
              {"id":2,"readable":true,"title":"Utan klipp","preview":"","explicit_lyrics":false,"artist":{"name":"B"},"album":{"title":"Y","cover_medium":"c2"}},
              {"id":3,"readable":false,"title":"Inte spelbar","preview":"https://cdnt-preview.dzcdn.net/c.mp3","explicit_lyrics":false,"artist":{"name":"C"},"album":{"title":"Z","cover_medium":"c3"}}
            ],"total":3}
            """;
        var (catalog, _) = CreateCatalog(FakeHttpHandler.Json(json));

        var tracks = (await catalog.SearchAsync(VeronicaMaggio)).Value;

        Assert.That(tracks, Is.EqualTo(new[] { new Track(1, "Med klipp", "A", "X", "c1", true) }));
    }

    [Test]
    public async Task Search_WithNoHits_ReturnsAnEmptyList()
    {
        var (catalog, _) = CreateCatalog(FakeHttpHandler.Json("""{"data":[],"total":0}"""));

        var result = await catalog.SearchAsync(VeronicaMaggio);

        Assert.That(result.Value, Is.Empty);
    }

    [TestCase(800, "music.unavailable")]
    [TestCase(500, "music.unavailable")]
    [TestCase(4, "music.busy")]
    public async Task Search_DeezerErrorInsideStatus200_BecomesAResultError(int code, string expected)
    {
        var (catalog, _) = CreateCatalog(FakeHttpHandler.Json($$$"""{"error":{"type":"Exception","message":"fel","code":{{{code}}}}}"""));

        var result = await catalog.SearchAsync(VeronicaMaggio);

        Assert.That(result.Error?.Code, Is.EqualTo(expected));
    }

    [Test]
    public async Task Search_WhenTheNetworkFails_ReturnsUnavailable()
    {
        var (catalog, _) = CreateCatalog(new FakeHttpHandler(_ => throw new HttpRequestException("nere")));

        var result = await catalog.SearchAsync(VeronicaMaggio);

        Assert.That(result.Error, Is.EqualTo(MusicErrors.Unavailable));
    }

    [Test]
    public async Task Search_WhenDeezerTimesOut_ReturnsUnavailable()
    {
        var (catalog, _) = CreateCatalog(new FakeHttpHandler(_ => throw new TaskCanceledException("timeout")));

        var result = await catalog.SearchAsync(VeronicaMaggio);

        Assert.That(result.Error, Is.EqualTo(MusicErrors.Unavailable));
    }

    [Test]
    public async Task Search_HitWithoutArtistOrAlbum_IsLeftOutInsteadOfCrashing()
    {
        var json = """
            {"data":[
              {"id":1,"readable":true,"title":"Hel","preview":"p","explicit_lyrics":false,"artist":{"name":"A"},"album":{"title":"X","cover_medium":"c1"}},
              {"id":2,"readable":true,"title":"Utan artist","preview":"p","explicit_lyrics":false,"album":{"title":"Y","cover_medium":"c2"}},
              {"id":3,"readable":true,"title":"Utan album","preview":"p","explicit_lyrics":false,"artist":{"name":"C"}}
            ],"total":3}
            """;
        var (catalog, _) = CreateCatalog(FakeHttpHandler.Json(json));

        var tracks = (await catalog.SearchAsync(VeronicaMaggio)).Value;

        Assert.That(tracks.Select(track => track.Title), Is.EqualTo(new[] { "Hel" }));
    }

    [Test]
    public async Task Search_WithBrokenJson_ReturnsUnavailable()
    {
        var (catalog, _) = CreateCatalog(FakeHttpHandler.Json("<html>inte json</html>"));

        var result = await catalog.SearchAsync(VeronicaMaggio);

        Assert.That(result.Error, Is.EqualTo(MusicErrors.Unavailable));
    }

    private static (DeezerMusicCatalog Catalog, FakeHttpHandler Handler) CreateCatalog(FakeHttpHandler handler)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.deezer.com/") };
        return (new DeezerMusicCatalog(http, NullLogger<DeezerMusicCatalog>.Instance), handler);
    }
}
