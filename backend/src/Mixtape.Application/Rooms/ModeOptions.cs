using System.Text.Json.Serialization;
using Mixtape.Domain.Games.AuxCord;
using Mixtape.Domain.Games.DropTheNeedle;

namespace Mixtape.Application.Rooms;

// Ett spelläge som värden kan välja, med gränsen för antal spelare och valen för inställningarna, så att klienten inte har egna kopior.
[JsonPolymorphic(TypeDiscriminatorPropertyName = "mode")]
[JsonDerivedType(typeof(AuxCordModeOptions), "auxCord")]
[JsonDerivedType(typeof(DropTheNeedleModeOptions), "dropTheNeedle")]
public abstract record ModeOptions(int MinPlayers)
{
    // I den ordning lägena visas i lobbyn.
    public static readonly IReadOnlyList<ModeOptions> All =
    [
        new AuxCordModeOptions(AuxCordSettings.Default.MinPlayers, AuxCordSettings.RoundOptions, AuxCordSettings.ClipSecondsOptions, AuxCordSettings.SubmittingSecondsOptions),
        new DropTheNeedleModeOptions(DropTheNeedleSettings.Default.MinPlayers, DropTheNeedleSettings.SongOptions, DropTheNeedleSettings.ClipSecondsOptions),
    ];
}
