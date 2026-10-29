namespace Mixtape.Domain.Rooms;

// Varför spelet står still. Speglas av pauseReason i hubContract.ts, så att mobilen kan säga rätt sak.
public enum PauseReason
{
    HostAway,
    PausedByHost,
}
