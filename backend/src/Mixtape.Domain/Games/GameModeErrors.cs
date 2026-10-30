using Mixtape.Domain.Common;

namespace Mixtape.Domain.Games;

public static class GameModeErrors
{
    // Låtarna hämtas innan rummet låses, och värden hann byta läge eller kategori under tiden.
    public static readonly Error SettingsChanged = new("mode.settings_changed", "Inställningarna ändrades. Försök igen.");
}
