namespace Mixtape.Infrastructure.Music.Deezer;

// Svaret från /track/{id}. En låt som inte finns ger bara Error, med koden 800.
internal sealed record DeezerTrackResponse(long Id, bool Readable, string? Title, string? Preview, DeezerArtist? Artist, DeezerAlbum? Album, DeezerError? Error)
    : IDeezerResponse;
