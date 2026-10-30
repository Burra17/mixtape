namespace Mixtape.Domain.Rooms;

// Ett spelläge. Rummet äger spelarna, klockan, pausen och ställningen, och spelet äger sina faser och sina regler.
// Ett nytt spel skapas för varje omgång och lever till "Spela igen", så att slutställningen kan visa den sista rundan.
public interface IGame
{
    // Det spelet drog när det skapades, som promptarna, så att rummet kan minnas det under kvällen och nästa spel kan dra annat.
    IReadOnlyCollection<string> UsedContent { get; }

    // players är rummets spelare, som inte ändras under spelet. Spelet delar ut poäng till dem.
    Step Start(IReadOnlyList<Player> players);

    // Fasen är slut, vid sin deadline eller när alla är klara.
    Step CompletePhase();

    // connected är de anslutna spelarna, så att en mobil som är borta inte hindrar de andra.
    bool IsEveryoneDone(IReadOnlyList<Player> connected);

    // Värden avslutar i förtid. Bara färdiga rundor ska räknas i slutställningen.
    void AbortRound();
}
