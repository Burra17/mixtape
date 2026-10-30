using Mixtape.Application.Music;
using Mixtape.Domain.Common;
using Mixtape.Infrastructure.Categories;

namespace Mixtape.Tests.Application;

public sealed class CategorySongsTests
{
    private static readonly CategoryCatalog Categories = new([new Category("00s", "00-tal", [1]), new Category("80s", "80-tal", [2, 3])]);

    [Test]
    public async Task Load_ACategoryWithTwoPlaylists_GivesTheSongsOfBoth()
    {
        var catalog = new PlaylistCatalog();

        var songs = (await new CategorySongs(Categories, catalog).LoadAsync("80s")).Value;

        Assert.That(songs.Select(song => song.Id), Is.EqualTo(new long[] { 20, 30 }));
    }

    [Test]
    public async Task Load_WhenAPlaylistFails_ReturnsSongsUnavailable()
    {
        var catalog = new PlaylistCatalog { Failing = 3 };

        var result = await new CategorySongs(Categories, catalog).LoadAsync("80s");

        Assert.That(result.Error, Is.EqualTo(MusicErrors.SongsUnavailable));
    }

    [Test]
    public async Task Load_AnUnknownCategory_ReturnsCategoryNotFound()
    {
        var result = await new CategorySongs(Categories, new PlaylistCatalog()).LoadAsync("disco");

        Assert.That(result.Error, Is.EqualTo(MusicErrors.CategoryNotFound));
    }

    // Varje spellista har en låt vars id är spellistans id gånger tio.
    private sealed class PlaylistCatalog : IMusicCatalog
    {
        public long? Failing { get; init; }

        public Task<Result<IReadOnlyList<Track>>> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Result<PlayableTrack>> GetPlayableTrackAsync(long trackId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Result<IReadOnlyList<PlayableTrack>>> GetPlaylistTracksAsync(long playlistId, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<PlayableTrack> tracks = [new PlayableTrack(playlistId * 10, $"Låt {playlistId}", "Artist", "Album", "omslag", "klipp")];
            return Task.FromResult(playlistId == Failing ? Result<IReadOnlyList<PlayableTrack>>.Failure(MusicErrors.Unavailable) : Result<IReadOnlyList<PlayableTrack>>.Success(tracks));
        }
    }
}
