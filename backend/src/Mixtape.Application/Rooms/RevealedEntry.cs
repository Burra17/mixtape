namespace Mixtape.Application.Rooms;

// Ett bidrag i avslöjandet: låten, vem som valde den och hur många röster den fick. Lämnar servern först när röstningen är slut. Speglas i hubContract.ts.
public sealed record RevealedEntry(int Number, string Title, string Artist, string CoverUrl, string PlayerId, int Votes);
