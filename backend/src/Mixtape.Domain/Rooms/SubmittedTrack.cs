namespace Mixtape.Domain.Rooms;

// Det rummet behöver veta om en inskickad låt för att känna igen dubbletter. Omslag och klipp hämtas färska vid uppspelningen.
public sealed record SubmittedTrack(long Id, string Title, string Artist)
{
    // Samma låt på singeln och på albumet har olika id men samma titel och artist. Olika versioner, som "Live", har olika titlar och räknas som olika låtar.
    public bool IsSameSongAs(SubmittedTrack other) =>
        Id == other.Id || (Normalize(Title) == Normalize(other.Title) && Normalize(Artist) == Normalize(other.Artist));

    private static string Normalize(string text) => string.Join(' ', text.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
}
