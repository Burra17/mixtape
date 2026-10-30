namespace Mixtape.Application.Rooms;

public sealed record DropTheNeedleSettingsState(int Songs, int ClipSeconds, int ThinkingSeconds, string CategoryId) : SettingsState;
