using Mixtape.Domain.Common;

namespace Mixtape.Domain.Games;

public static class GameModeErrors
{
    // Läget går att välja innan spelet finns. Tas bort när Drop the Needle går att spela (#70).
    public static readonly Error NotReady = new("mode.not_ready", "Drop the Needle är inte klart än.");
}
