using System.Text.Json.Serialization;
using Mixtape.Domain.Games;
using Mixtape.Domain.Games.AuxCord;
using Mixtape.Domain.Games.DropTheNeedle;

namespace Mixtape.Application.Rooms;

// Inställningarna som värden valt i lobbyn för det valda läget. mode skiljer lägena åt, som i unionen i hubContract.ts.
[JsonPolymorphic(TypeDiscriminatorPropertyName = "mode")]
[JsonDerivedType(typeof(AuxCordSettingsState), "auxCord")]
[JsonDerivedType(typeof(DropTheNeedleSettingsState), "dropTheNeedle")]
public abstract record SettingsState
{
    public static SettingsState From(IGameSettings settings) =>
        settings switch
        {
            AuxCordSettings aux => new AuxCordSettingsState(aux.Rounds, aux.ClipSeconds, aux.SubmittingSeconds),
            DropTheNeedleSettings needle => new DropTheNeedleSettingsState(needle.Songs, needle.ClipSeconds, needle.CategoryId),
            _ => throw new ArgumentOutOfRangeException(nameof(settings), settings.Mode, "Spelläget saknar inställningar i kontraktet."),
        };
}
