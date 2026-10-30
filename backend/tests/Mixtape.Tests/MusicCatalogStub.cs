using Mixtape.Application.Music;
using Mixtape.Domain.Common;

namespace Mixtape.Tests;

// Grunden för testernas musikkällor. Ett test skriver bara över det det använder, och resten säger tydligt ifrån om det anropas.
public abstract class MusicCatalogStub : IMusicCatalog
{
    public virtual Task<Result<IReadOnlyList<Track>>> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public virtual Task<Result<PlayableTrack>> GetPlayableTrackAsync(long trackId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public virtual Task<Result<IReadOnlyList<PlayableTrack>>> GetPlaylistTracksAsync(long playlistId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
