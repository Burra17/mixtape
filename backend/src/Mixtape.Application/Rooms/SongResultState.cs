using Mixtape.Domain.Games.DropTheNeedle;

namespace Mixtape.Application.Rooms;

// En spelares resultat för låten. Gissningen går till alla, eftersom den inte är hemlig efter avslöjandet och mobilen då plockar ut sin egen. Speglas i hubContract.ts.
public sealed record SongResultState(string PlayerId, GuessScore Score, bool Fastest, int Points, RevealedSong? Guess)
{
    public static SongResultState From(SongResult result) =>
        new(result.PlayerId, result.Score, result.Fastest, result.Points, result.Guess is { } guess ? RevealedSong.From(guess) : null);
}
