namespace Mixtape.Domain.Rooms;

// Nedräkningen för fasen som pågår. Under paus fryses tiden som är kvar, så att fasen får tillbaka den när storskärmen kommer tillbaka.
public sealed class PhaseClock
{
    private DateTimeOffset? _endsAt;
    private TimeSpan? _frozen;
    private bool _isPaused;

    // En fas som börjar under paus väntar med hela sin tid tills pausen hävs.
    public void Start(TimeSpan duration, DateTimeOffset now)
    {
        _endsAt = _isPaused ? null : now + duration;
        _frozen = _isPaused ? duration : null;
    }

    // För faser utan deadline, som lobbyn och slutställningen.
    public void Stop()
    {
        _endsAt = null;
        _frozen = null;
    }

    public void Pause(DateTimeOffset now)
    {
        if (_isPaused)
        {
            return;
        }

        _isPaused = true;
        if (_endsAt is { } endsAt)
        {
            _frozen = Max(endsAt - now, TimeSpan.Zero);
            _endsAt = null;
        }
    }

    public void Resume(DateTimeOffset now)
    {
        if (!_isPaused)
        {
            return;
        }

        _isPaused = false;
        if (_frozen is { } frozen)
        {
            _endsAt = now + frozen;
            _frozen = null;
        }
    }

    public bool IsExpired(DateTimeOffset now) => _endsAt is { } endsAt && now >= endsAt;

    // Null när ingen nedräkning pågår.
    public TimeSpan? Remaining(DateTimeOffset now) => _frozen ?? (_endsAt is { } endsAt ? Max(endsAt - now, TimeSpan.Zero) : null);

    private static TimeSpan Max(TimeSpan a, TimeSpan b) => a > b ? a : b;
}
