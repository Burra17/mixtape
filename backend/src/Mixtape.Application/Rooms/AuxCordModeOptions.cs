namespace Mixtape.Application.Rooms;

public sealed record AuxCordModeOptions(
    int MinPlayers,
    IReadOnlyList<int> RoundOptions,
    IReadOnlyList<int> ClipSecondsOptions,
    IReadOnlyList<int> SubmittingSecondsOptions) : ModeOptions(MinPlayers);
