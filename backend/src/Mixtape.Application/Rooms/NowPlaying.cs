namespace Mixtape.Application.Rooms;

// Klippet som spelas, som alla får se. Ingen länk och inget som pekar ut vem som valde låten. Speglas i hubContract.ts.
public sealed record NowPlaying(int Index, int Count, string Title, string Artist, string Album, string CoverUrl);
