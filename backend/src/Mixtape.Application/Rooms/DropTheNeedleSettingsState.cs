namespace Mixtape.Application.Rooms;

public sealed record DropTheNeedleSettingsState(int Songs, int ClipSeconds, int ThinkingSeconds, bool FastestBonus, string CategoryId) : SettingsState;
