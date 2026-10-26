namespace Mixtape.Application.Rooms;

// Ett bidrag att rösta på, som alla får se. Number är platsen i uppspelningen, 1 för det första klippet. Inget som pekar ut vem som valde låten. Speglas i hubContract.ts.
public sealed record BallotEntry(int Number, string Title, string Artist, string CoverUrl);
