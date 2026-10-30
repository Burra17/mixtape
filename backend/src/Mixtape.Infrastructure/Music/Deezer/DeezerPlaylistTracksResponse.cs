namespace Mixtape.Infrastructure.Music.Deezer;

// Svaret från /playlist/{id}/tracks, en sida i taget. Next saknas på sista sidan. En spellista som inte finns ger bara Error, med koden 800.
internal sealed record DeezerPlaylistTracksResponse(IReadOnlyList<DeezerTrack>? Data, string? Next, DeezerError? Error) : IDeezerResponse;
