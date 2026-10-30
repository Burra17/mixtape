namespace Mixtape.Domain.Rooms;

// Spelets svar när en fas börjar eller tar slut: nästa fas med sin tid, eller att spelet är slut. Rummet startar klockan med tiden.
public sealed record Step
{
    public static readonly Step Finished = new((TimeSpan?)null);

    private Step(TimeSpan? duration) => Duration = duration;

    // Null när spelet är slut.
    public TimeSpan? Duration { get; }

    public static Step Next(TimeSpan duration) => new(duration);
}
