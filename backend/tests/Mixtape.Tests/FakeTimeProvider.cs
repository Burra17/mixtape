namespace Mixtape.Tests;

// En klocka som testerna spolar fram själva, så att ett test om en halvtimme inte behöver vänta en halvtimme.
public sealed class FakeTimeProvider(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset _now = start;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now += by;
}
