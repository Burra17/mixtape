using Mixtape.Domain.Music;

namespace Mixtape.Domain.Games;

// Klippet som storskärmen ska spela. Number är 1 för det första klippet i rundan eller spelet.
public sealed record CurrentClip(int Number, SubmittedTrack Track, DateTimeOffset LinkFetchedAt)
{
    // Deezers länk gäller i 15 minuter. Efter 10 hämtas en ny innan klippet spelas, så att den inte hinner gå ut under klippet.
    public static readonly TimeSpan LinkMaxAge = TimeSpan.FromMinutes(10);

    public bool NeedsNewLink(DateTimeOffset now) => now - LinkFetchedAt >= LinkMaxAge;
}
