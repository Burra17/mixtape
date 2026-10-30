namespace Mixtape.Domain.Music;

// En inskickad låt med det som storskärmen visar och spelar. Klipplänken gäller i 15 minuter och förnyas efter en lång paus.
// Contributors är alla medverkande artister, som Deezer bara ger när en enskild låt hämtas. Null när de inte är hämtade.
public sealed record SubmittedTrack(long Id, string Title, string Artist, string Album, string CoverUrl, string PreviewUrl, IReadOnlyList<string>? Contributors = null)
{
    // Samma låt på singeln och på albumet har olika id men samma titel och artist. Olika versioner, som "Live", har olika titlar och räknas som olika låtar.
    public bool IsSameSongAs(SubmittedTrack other) =>
        Id == other.Id || (Normalize(Title) == Normalize(other.Title) && Normalize(Artist) == Normalize(other.Artist));

    // Allt blanktecken räknas, som i cachenyckeln för sökningen.
    private static string Normalize(string text) => string.Join(' ', text.Split(default(char[]), StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
}
