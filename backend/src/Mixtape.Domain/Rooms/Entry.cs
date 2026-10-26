namespace Mixtape.Domain.Rooms;

// Ett bidrag i rundan. Vem som skickade in vad stannar på servern till avslöjandet.
public sealed record Entry(string PlayerId, SubmittedTrack Track, DateTimeOffset LinkFetchedAt)
{
    // Deezers länk gäller i 15 minuter. Efter 10 hämtas en ny innan klippet spelas, så att den inte hinner gå ut under klippet.
    public static readonly TimeSpan LinkMaxAge = TimeSpan.FromMinutes(10);

    public bool NeedsNewLink(DateTimeOffset now) => now - LinkFetchedAt >= LinkMaxAge;
}
