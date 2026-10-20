namespace Mixtape.Infrastructure.Music.Deezer;

// CoverMedium (250 px) räcker för träfflistan i mobilen. CoverXl (1000 px) visas på storskärmen.
internal sealed record DeezerAlbum(string? Title, string? CoverMedium, string? CoverXl);
