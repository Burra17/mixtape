using Mixtape.Domain.Rooms;

namespace Mixtape.Tests.Domain;

public sealed class PhaseClockTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 20, 20, 0, 0, TimeSpan.Zero);

    private PhaseClock _clock = null!;

    [SetUp]
    public void SetUp()
    {
        _clock = new PhaseClock();
    }

    [Test]
    public void Remaining_BeforeStart_IsNull()
    {
        Assert.That(_clock.Remaining(Now), Is.Null);
        Assert.That(_clock.IsExpired(Now), Is.False);
    }

    [Test]
    public void Start_CountsDownToTheDeadline()
    {
        _clock.Start(TimeSpan.FromSeconds(45), Now);

        Assert.That(_clock.Remaining(Now.AddSeconds(10)), Is.EqualTo(TimeSpan.FromSeconds(35)));
        Assert.That(_clock.IsExpired(Now.AddSeconds(44.9)), Is.False);
        Assert.That(_clock.IsExpired(Now.AddSeconds(45)), Is.True);
    }

    [Test]
    public void Remaining_AfterTheDeadline_IsZero()
    {
        _clock.Start(TimeSpan.FromSeconds(45), Now);

        Assert.That(_clock.Remaining(Now.AddMinutes(5)), Is.EqualTo(TimeSpan.Zero));
    }

    [Test]
    public void Pause_FreezesTheRemainingTime()
    {
        _clock.Start(TimeSpan.FromSeconds(45), Now);

        _clock.Pause(Now.AddSeconds(33));

        Assert.That(_clock.Remaining(Now.AddMinutes(10)), Is.EqualTo(TimeSpan.FromSeconds(12)));
        Assert.That(_clock.IsExpired(Now.AddMinutes(10)), Is.False);
    }

    [Test]
    public void Resume_GivesBackTheTimeThatWasLeft()
    {
        _clock.Start(TimeSpan.FromSeconds(45), Now);
        _clock.Pause(Now.AddSeconds(33));

        var resumedAt = Now.AddMinutes(10);
        _clock.Resume(resumedAt);

        Assert.That(_clock.IsExpired(resumedAt.AddSeconds(11.9)), Is.False);
        Assert.That(_clock.IsExpired(resumedAt.AddSeconds(12)), Is.True);
    }

    [Test]
    public void Pause_Twice_KeepsTheFirstFrozenTime()
    {
        _clock.Start(TimeSpan.FromSeconds(45), Now);
        _clock.Pause(Now.AddSeconds(33));

        _clock.Pause(Now.AddSeconds(40));

        Assert.That(_clock.Remaining(Now.AddSeconds(40)), Is.EqualTo(TimeSpan.FromSeconds(12)));
    }

    [Test]
    public void Start_WhilePaused_WaitsWithTheWholeDurationUntilResumed()
    {
        _clock.Pause(Now);

        _clock.Start(TimeSpan.FromSeconds(20), Now.AddSeconds(5));
        Assert.That(_clock.Remaining(Now.AddMinutes(1)), Is.EqualTo(TimeSpan.FromSeconds(20)));

        _clock.Resume(Now.AddMinutes(1));
        Assert.That(_clock.IsExpired(Now.AddMinutes(1).AddSeconds(20)), Is.True);
    }

    [Test]
    public void Stop_EndsTheCountdown()
    {
        _clock.Start(TimeSpan.FromSeconds(45), Now);

        _clock.Stop();

        Assert.That(_clock.Remaining(Now), Is.Null);
        Assert.That(_clock.IsExpired(Now.AddMinutes(1)), Is.False);
    }
}
