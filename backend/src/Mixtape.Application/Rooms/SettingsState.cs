namespace Mixtape.Application.Rooms;

// Inställningarna som värden valt i lobbyn. Speglas i hubContract.ts.
public sealed record SettingsState(int Rounds, int ClipSeconds);
