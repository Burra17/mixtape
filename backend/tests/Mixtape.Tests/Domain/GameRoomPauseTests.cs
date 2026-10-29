using Mixtape.Domain.Rooms;

namespace Mixtape.Tests.Domain;

public sealed class GameRoomPauseTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 29, 20, 0, 0, TimeSpan.Zero);

    private static readonly TimeSpan Submitting = GameSettings.Default.SubmittingDuration;

    private GameRoom _room = null!;

    [SetUp]
    public void SetUp()
    {
        _room = GameRoom.Create(RoomCode.Parse("BCDF")!, Random.Shared);
        for (var i = 1; i <= 3; i++)
        {
            _room.Join($"Spelare {i}");
        }
    }

    [Test]
    public void Pause_FreezesThePhaseUntilTheHostContinues()
    {
        Start();
        _room.Pause(_room.HostToken, Now.AddSeconds(30));

        Assert.That(_room.IsPaused, Is.True);
        Assert.That(_room.PauseReason, Is.EqualTo(PauseReason.PausedByHost));
        Assert.That(_room.Tick(Now.AddMinutes(10)), Is.False);
        Assert.That(_room.RemainingTime(Now.AddMinutes(10)), Is.EqualTo(Submitting - TimeSpan.FromSeconds(30)));

        var back = Now.AddMinutes(10);
        _room.Continue(_room.HostToken, back);

        Assert.That(_room.IsPaused, Is.False);
        Assert.That(_room.PauseReason, Is.Null);
        Assert.That(_room.Tick(back + Submitting - TimeSpan.FromSeconds(31)), Is.False);
        Assert.That(_room.Tick(back + Submitting - TimeSpan.FromSeconds(30)), Is.True);
    }

    [Test]
    public void Pause_KeepsThePhaseWhenEveryoneIsDone()
    {
        Start();
        _room.Pause(_room.HostToken, Now);

        foreach (var player in _room.Players)
        {
            _room.SubmitTrack(player.Id, TestTrack.Of(player.ColorIndex, $"Låt {player.ColorIndex}", "Artist"), Now);
        }

        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Submitting));
        Assert.That(_room.Tick(Now.AddSeconds(1)), Is.False);

        _room.Continue(_room.HostToken, Now.AddSeconds(2));

        Assert.That(_room.Tick(Now.AddSeconds(2)), Is.True);
        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Playing));
    }

    [Test]
    public void ResumeHost_KeepsAPauseTheHostMade()
    {
        Start();
        _room.Pause(_room.HostToken, Now);
        _room.DisconnectHost(Now.AddSeconds(5));

        Assert.That(_room.PauseReason, Is.EqualTo(PauseReason.HostAway));

        _room.ResumeHost(_room.HostToken, Now.AddMinutes(1));

        Assert.That(_room.PauseReason, Is.EqualTo(PauseReason.PausedByHost));
        Assert.That(_room.Tick(Now.AddMinutes(30)), Is.False);
        Assert.That(_room.RemainingTime(Now.AddMinutes(30)), Is.EqualTo(Submitting));
    }

    [Test]
    public void Continue_WithTheHostAway_KeepsTheClockFrozen()
    {
        Start();
        _room.Pause(_room.HostToken, Now);
        _room.DisconnectHost(Now);

        _room.Continue(_room.HostToken, Now.AddMinutes(1));

        Assert.That(_room.PauseReason, Is.EqualTo(PauseReason.HostAway));
        Assert.That(_room.RemainingTime(Now.AddMinutes(30)), Is.EqualTo(Submitting));
    }

    [Test]
    public void Pause_Twice_ChangesTheRevisionOnce()
    {
        Start();
        _room.Pause(_room.HostToken, Now);
        var paused = _room.Revision;

        Assert.That(_room.Pause(_room.HostToken, Now.AddSeconds(5)).IsSuccess, Is.True);
        Assert.That(_room.Revision, Is.EqualTo(paused));
        Assert.That(_room.RemainingTime(Now.AddSeconds(5)), Is.EqualTo(Submitting));
    }

    [Test]
    public void Continue_WithoutAPause_ChangesNothing()
    {
        Start();
        var before = _room.Revision;

        Assert.That(_room.Continue(_room.HostToken, Now.AddSeconds(5)).IsSuccess, Is.True);
        Assert.That(_room.Revision, Is.EqualTo(before));
        Assert.That(_room.RemainingTime(Now.AddSeconds(5)), Is.EqualTo(Submitting - TimeSpan.FromSeconds(5)));
    }

    [Test]
    public void Pause_InTheLobby_ReturnsWrongPhase()
    {
        Assert.That(_room.Pause(_room.HostToken, Now).Error, Is.EqualTo(RoomErrors.WrongPhase));
        Assert.That(_room.IsPaused, Is.False);
    }

    [Test]
    public void Pause_AtGameOver_ReturnsWrongPhase()
    {
        _room.UpdateSettings(_room.HostToken, 3, null, null);
        Start();
        // Inga bidrag, så varje runda hoppas över vid sin deadline.
        for (var round = 1; round <= 3; round++)
        {
            _room.Tick(Now + Submitting * round);
        }

        Assert.That(_room.Phase, Is.EqualTo(GamePhase.GameOver));
        Assert.That(_room.Pause(_room.HostToken, Now.AddHours(1)).Error, Is.EqualTo(RoomErrors.WrongPhase));
    }

    [TestCase(null)]
    [TestCase("fel")]
    public void Pause_WithWrongHostToken_ReturnsRoomNotFound(string? token)
    {
        Start();

        Assert.That(_room.Pause(token, Now).Error, Is.EqualTo(RoomErrors.NotFound));
        Assert.That(_room.IsPaused, Is.False);
    }

    [TestCase(null)]
    [TestCase("fel")]
    public void Continue_WithWrongHostToken_ReturnsRoomNotFound(string? token)
    {
        Start();
        _room.Pause(_room.HostToken, Now);

        Assert.That(_room.Continue(token, Now).Error, Is.EqualTo(RoomErrors.NotFound));
        Assert.That(_room.IsPaused, Is.True);
    }

    private void Start() =>
        _room.StartGame(_room.HostToken, Enumerable.Range(1, _room.Settings.Rounds).Select(round => $"Prompt {round}").ToList(), Now);
}
