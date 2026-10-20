using Mixtape.Domain.Common;

namespace Mixtape.Application.Music;

// Felen kommer från musikkällan och sökordet, inte från spelets regler, så de ligger i Application och inte i Domain.
public static class MusicErrors
{
    public static readonly Error InvalidQuery = new("music.invalid_query", "Skriv något att söka på.");

    public static readonly Error Unavailable = new("music.unavailable", "Sökningen fungerar inte just nu. Försök igen.");

    public static readonly Error Busy = new("music.busy", "Många söker samtidigt. Vänta några sekunder och försök igen.");

    // Låten har försvunnit eller saknar klipp. Storskärmen kan hoppa över den i stället för att stanna.
    public static readonly Error TrackUnavailable = new("music.track_unavailable", "Låten går inte att spela längre.");
}
