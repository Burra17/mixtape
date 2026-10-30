namespace Mixtape.Application.Rooms;

// Kontraktets fas: rummets läge, och under spelet spelets fas. Speglas av PHASES i hubContract.ts, och mapparna i frontend/src/features heter som faserna.
public enum GamePhase
{
    Lobby,
    Submitting,
    Playing,
    Voting,
    Revealing,
    Guessing,
    SongReveal,
    GameOver,
}
