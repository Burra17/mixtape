namespace Mixtape.Application.Music;

// En sökträff utan klipplänk, eftersom länken slutar gälla efter 15 minuter och hämtas färsk i #15. Speglas i hubContract.ts.
public sealed record Track(long Id, string Title, string Artist, string Album, string CoverUrl, bool IsExplicit);
