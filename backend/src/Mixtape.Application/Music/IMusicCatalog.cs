using Mixtape.Domain.Common;

namespace Mixtape.Application.Music;

// Musikkällan. I dag Deezer. En annan källa, som iTunes, blir en ny klass bakom samma interface.
public interface IMusicCatalog
{
    // Så många träffar visar mobilen, oavsett vilken källa som söker.
    const int MaxResults = 8;

    // Högst MaxResults träffar som alla har ett klipp. Inga träffar är en lyckad, tom lista.
    Task<Result<IReadOnlyList<Track>>> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default);
}
