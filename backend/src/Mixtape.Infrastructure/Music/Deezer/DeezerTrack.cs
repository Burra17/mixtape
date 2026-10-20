namespace Mixtape.Infrastructure.Music.Deezer;

// Bara fälten som Mixtape använder. Artist och album kan saknas i ett trasigt svar, och då filtreras träffen bort.
internal sealed record DeezerTrack(long Id, bool Readable, string Title, string? Preview, bool ExplicitLyrics, DeezerArtist? Artist, DeezerAlbum? Album);
