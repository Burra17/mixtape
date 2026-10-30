namespace Mixtape.Infrastructure.Music.Deezer;

// Svaret från /track/{id}. En låt som inte finns ger bara Error, med koden 800. Contributors finns bara här, inte i sökningen eller spellistorna.
internal sealed record DeezerTrackResponse(long Id, bool Readable, string? Title, string? Preview, DeezerArtist? Artist, DeezerAlbum? Album, IReadOnlyList<DeezerArtist>? Contributors, DeezerError? Error)
    : IDeezerResponse;
