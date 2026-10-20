namespace Mixtape.Infrastructure.Music.Deezer;

// Bara fälten som Mixtape använder. Resten av Deezers svar ignoreras.
internal sealed record DeezerTrack(long Id, bool Readable, string Title, string? Preview, bool ExplicitLyrics, DeezerArtist Artist, DeezerAlbum Album);
