using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Mixtape.Application.Music;
using Mixtape.Infrastructure.Music.Deezer;

namespace Mixtape.Tests.Infrastructure;

public sealed class DeezerPlaylistTests
{
    [Test]
    public async Task GetPlaylistTracks_FollowsThePagesAndLeavesOutTracksThatCannotBePlayed()
    {
        var firstPage = await Fixture("deezer-playlist-1470022445-page1.json");
        var lastPage = await Fixture("deezer-playlist-1470022445-last.json");
        var handler = new FakeHttpHandler(request => Json(request.RequestUri!.Query.Contains("index=0") ? firstPage : lastPage));
        var catalog = new DeezerMusicCatalog(new HttpClient(handler) { BaseAddress = new Uri("https://api.deezer.com/") }, NullLogger<DeezerMusicCatalog>.Instance);

        var tracks = (await catalog.GetPlaylistTracksAsync(1470022445)).Value;

        // Sista sidan har bara September, som inte går att spela i Sverige.
        Assert.That(tracks.Select(track => track.Title), Is.EqualTo(new[] { "Dreams", "Highway to Hell", "Have You Ever Seen The Rain" }));
        Assert.That(tracks[0].Artist, Is.EqualTo("Fleetwood Mac"));
        Assert.That(tracks[0].PreviewUrl, Does.StartWith("https://"));
        Assert.That(handler.Requests.Select(uri => uri.PathAndQuery), Is.EqualTo(new[]
        {
            "/playlist/1470022445/tracks?limit=100&index=0",
            "/playlist/1470022445/tracks?limit=100&index=3",
        }));
    }

    [Test]
    public async Task GetPlaylistTracks_LeavesOutAPlayableTrackWithoutPreview()
    {
        var json = """
            {"data":[
              {"id":1,"readable":true,"title":"Med klipp","preview":"https://cdnt-preview.dzcdn.net/a.mp3","explicit_lyrics":false,"artist":{"name":"A"},"album":{"title":"X","cover_xl":"xl1"}},
              {"id":2,"readable":true,"title":"Utan klipp","preview":"","explicit_lyrics":false,"artist":{"name":"B"},"album":{"title":"Y","cover_xl":"xl2"}}
            ],"total":2}
            """;
        var catalog = new DeezerMusicCatalog(new HttpClient(FakeHttpHandler.Json(json)) { BaseAddress = new Uri("https://api.deezer.com/") }, NullLogger<DeezerMusicCatalog>.Instance);

        var tracks = (await catalog.GetPlaylistTracksAsync(1)).Value;

        Assert.That(tracks, Is.EqualTo(new[] { new PlayableTrack(1, "Med klipp", "A", "X", "xl1", "https://cdnt-preview.dzcdn.net/a.mp3") }));
    }

    [Test]
    public async Task GetPlaylistTracks_ThatDoesNotExist_ReturnsUnavailable()
    {
        var handler = FakeHttpHandler.Json("""{"error":{"type":"DataException","message":"no data","code":800}}""");
        var catalog = new DeezerMusicCatalog(new HttpClient(handler) { BaseAddress = new Uri("https://api.deezer.com/") }, NullLogger<DeezerMusicCatalog>.Instance);

        var result = await catalog.GetPlaylistTracksAsync(1);

        Assert.That(result.Error, Is.EqualTo(MusicErrors.Unavailable));
    }

    private static Task<string> Fixture(string name) => File.ReadAllTextAsync(Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", name));

    private static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
}
