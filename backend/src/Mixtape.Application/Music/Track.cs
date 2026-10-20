namespace Mixtape.Application.Music;

// En sökträff. Title har med versionen, som "(Radio Edit)", och albumet skiljer versionerna åt. Klipplänken finns inte här,
// eftersom den slutar gälla efter 15 minuter och hämtas färsk inför uppspelningen (#15). Speglas i hubContract.ts.
public sealed record Track(long Id, string Title, string Artist, string Album, string CoverUrl, bool IsExplicit);
