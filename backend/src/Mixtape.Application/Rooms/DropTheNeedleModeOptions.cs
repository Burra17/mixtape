namespace Mixtape.Application.Rooms;

public sealed record DropTheNeedleModeOptions(int MinPlayers, IReadOnlyList<int> SongOptions, IReadOnlyList<int> ClipSecondsOptions, IReadOnlyList<int> ThinkingSecondsOptions) : ModeOptions(MinPlayers);
