namespace Mixtape.Application.Rooms;

public sealed record AuxCordSettingsState(int Rounds, int ClipSeconds, int SubmittingSeconds) : SettingsState;
