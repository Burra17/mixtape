namespace Mixtape.Application.Rooms;

public sealed record DropTheNeedleSettingsState(int Songs, int ClipSeconds, string CategoryId) : SettingsState;
