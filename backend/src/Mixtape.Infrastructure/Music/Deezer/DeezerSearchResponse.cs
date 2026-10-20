namespace Mixtape.Infrastructure.Music.Deezer;

// Deezer svarar med status 200 även vid fel. Då saknas Data och Error är satt.
internal sealed record DeezerSearchResponse(IReadOnlyList<DeezerTrack>? Data, DeezerError? Error);
