using Mixtape.Domain.Games.AuxCord;
using Mixtape.Domain.Games.DropTheNeedle;

namespace Mixtape.Domain.Games;

public static class GameModes
{
    // Aux Cord Battles är standard, eftersom det var spelets första läge.
    public const GameMode Default = GameMode.AuxCord;

    // Inställningarna ett läge får första gången det väljs i rummet.
    public static IGameSettings DefaultSettings(GameMode mode) =>
        mode switch
        {
            GameMode.AuxCord => AuxCordSettings.Default,
            GameMode.DropTheNeedle => DropTheNeedleSettings.Default,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Spelläget finns inte."),
        };
}
