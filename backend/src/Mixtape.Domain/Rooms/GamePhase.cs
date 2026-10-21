namespace Mixtape.Domain.Rooms;

// Speglas av PHASES i hubContract.ts, och mapparna i frontend/src/features heter som faserna.
public enum GamePhase
{
    Lobby,
    Submitting,
    Playing,
    Voting,
    Revealing,
    GameOver,
}
