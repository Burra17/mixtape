using Mixtape.Domain.Common;
using Mixtape.Domain.Music;

namespace Mixtape.Application.Music;

// Alla spelbara låtar i en kategori, från alla dess spellistor. Används när Drop the Needle startar (#70), utanför rummets lås.
public sealed class CategorySongs(ICategoryCatalog categories, IMusicCatalog catalog)
{
    public async Task<Result<IReadOnlyList<SubmittedTrack>>> LoadAsync(string categoryId, CancellationToken cancellationToken = default)
    {
        if (categories.Find(categoryId) is not { } category)
        {
            return MusicErrors.CategoryNotFound;
        }

        var songs = new List<SubmittedTrack>();
        foreach (var playlistId in category.Playlists)
        {
            var tracks = await catalog.GetPlaylistTracksAsync(playlistId, cancellationToken);
            if (!tracks.IsSuccess)
            {
                return MusicErrors.SongsUnavailable;
            }

            songs.AddRange(tracks.Value.Select(track => track.ToSubmittedTrack()));
        }

        return songs;
    }
}
