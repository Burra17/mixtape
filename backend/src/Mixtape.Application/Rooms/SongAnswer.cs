namespace Mixtape.Application.Rooms;

// Drop the Needles avslöjande: rätt svar och vad varje spelare fick, i spelarnas ordning. Lämnar servern först när gissningen är slut. Speglas i hubContract.ts.
public sealed record SongAnswer(RevealedSong Song, IReadOnlyList<SongResultState> Results);
