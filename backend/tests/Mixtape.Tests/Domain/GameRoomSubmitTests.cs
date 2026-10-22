using Mixtape.Domain.Rooms;

namespace Mixtape.Tests.Domain;

public sealed class GameRoomSubmitTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 20, 0, 0, TimeSpan.Zero);

    private GameRoom _room = null!;
    private Player _anna = null!;
    private Player _bertil = null!;
    private Player _cecilia = null!;

    [SetUp]
    public void SetUp()
    {
        _room = GameRoom.Create(RoomCode.Parse("BCDF")!);
        _anna = _room.Join("Anna").Value;
        _bertil = _room.Join("Bertil").Value;
        _cecilia = _room.Join("Cecilia").Value;
    }

    [Test]
    public void SubmitTrack_DuringSubmitting_LocksTheEntry()
    {
        Start();

        var result = _room.SubmitTrack(_anna.Id, Track(1, "Jag kommer"), Now);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(_room.HasSubmitted(_anna.Id), Is.True);
        Assert.That(_room.Entries, Is.EqualTo(new[] { new Entry(_anna.Id, Track(1, "Jag kommer"), Now) }));
    }

    [Test]
    public void SubmitTrack_Twice_ReturnsAlreadySubmitted()
    {
        Start();
        _room.SubmitTrack(_anna.Id, Track(1, "Jag kommer"), Now);

        var result = _room.SubmitTrack(_anna.Id, Track(2, "Satan i gatan"), Now);

        Assert.That(result.Error, Is.EqualTo(EntryErrors.AlreadySubmitted));
        Assert.That(_room.Entries.Single().Track.Id, Is.EqualTo(1));
    }

    [Test]
    public void SubmitTrack_WithTheSameId_ReturnsTrackTaken()
    {
        Start();
        _room.SubmitTrack(_anna.Id, Track(1, "Jag kommer"), Now);

        Assert.That(_room.SubmitTrack(_bertil.Id, Track(1, "Jag kommer"), Now).Error, Is.EqualTo(EntryErrors.TrackTaken));
        Assert.That(_room.HasSubmitted(_bertil.Id), Is.False);
    }

    // Samma låt på singeln och på albumet har olika id.
    [Test]
    public void SubmitTrack_WithTheSameTitleAndArtistInOtherCase_ReturnsTrackTaken()
    {
        Start();
        _room.SubmitTrack(_anna.Id, TestTrack.Of(1, "Jag kommer", "Veronica Maggio"), Now);

        var result = _room.SubmitTrack(_bertil.Id, TestTrack.Of(2, " jag  KOMMER ", "veronica maggio"), Now);

        Assert.That(result.Error, Is.EqualTo(EntryErrors.TrackTaken));
    }

    [Test]
    public void SubmitTrack_WithTheSameTitleButOtherWhitespace_ReturnsTrackTaken()
    {
        Start();
        _room.SubmitTrack(_anna.Id, TestTrack.Of(1, "Jag kommer", "Veronica Maggio"), Now);

        var result = _room.SubmitTrack(_bertil.Id, TestTrack.Of(2, "Jag\tkommer\n", "Veronica\u00a0Maggio"), Now);

        Assert.That(result.Error, Is.EqualTo(EntryErrors.TrackTaken));
    }

    [Test]
    public void SubmitTrack_WithAnotherVersion_IsAccepted()
    {
        Start();
        _room.SubmitTrack(_anna.Id, TestTrack.Of(1, "Jag kommer", "Veronica Maggio"), Now);

        var result = _room.SubmitTrack(_bertil.Id, TestTrack.Of(2, "Jag kommer (Live)", "Veronica Maggio"), Now);

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public void SubmitTrack_InTheLobby_ReturnsWrongPhase()
    {
        Assert.That(_room.SubmitTrack(_anna.Id, Track(1, "Jag kommer"), Now).Error, Is.EqualTo(RoomErrors.WrongPhase));
    }

    [Test]
    public void SubmitTrack_AfterSubmitting_ReturnsWrongPhase()
    {
        Start();
        SubmitForAll();

        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Playing));
        Assert.That(_room.SubmitTrack(_anna.Id, Track(9, "Sent"), Now).Error, Is.EqualTo(RoomErrors.WrongPhase));
    }

    [Test]
    public void SubmitTrack_FromUnknownPlayer_ReturnsPlayerNotFound()
    {
        Start();

        Assert.That(_room.SubmitTrack("okänd", Track(1, "Jag kommer"), Now).Error, Is.EqualTo(PlayerErrors.NotFound));
    }

    [Test]
    public void CanChooseTrack_OnlyDuringSubmitting()
    {
        Assert.That(_room.CanChooseTrack().Error, Is.EqualTo(RoomErrors.WrongPhase));

        Start();

        Assert.That(_room.CanChooseTrack().IsSuccess, Is.True);
    }

    [Test]
    public void SubmitTrack_FromTheLastConnectedPlayer_MovesOnAtOnce()
    {
        Start();
        _room.SubmitTrack(_anna.Id, Track(1, "Ett"), Now);
        _room.SubmitTrack(_bertil.Id, Track(2, "Två"), Now);
        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Submitting));

        _room.SubmitTrack(_cecilia.Id, Track(3, "Tre"), Now.AddSeconds(5));

        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Playing));
        Assert.That(_room.RemainingTime(Now.AddSeconds(5)), Is.EqualTo(GameSettings.Default.ClipLength));
    }

    [Test]
    public void SubmitTrack_WithAPlayerAway_DoesNotWaitForThem()
    {
        Start();
        _room.Disconnect(_cecilia.Id, Now);

        _room.SubmitTrack(_anna.Id, Track(1, "Ett"), Now);
        _room.SubmitTrack(_bertil.Id, Track(2, "Två"), Now);

        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Playing));
    }

    [Test]
    public void Rejoin_DuringSubmitting_CountsThePlayerAgain()
    {
        Start();
        _room.Disconnect(_cecilia.Id, Now);
        _room.Rejoin(_cecilia.Token);

        _room.SubmitTrack(_anna.Id, Track(1, "Ett"), Now);
        _room.SubmitTrack(_bertil.Id, Track(2, "Två"), Now);

        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Submitting));
    }

    [Test]
    public void Tick_WhenTheLastPlayerWhoHadNotSubmittedLeaves_MovesOn()
    {
        Start();
        _room.SubmitTrack(_anna.Id, Track(1, "Ett"), Now);
        _room.SubmitTrack(_bertil.Id, Track(2, "Två"), Now);

        _room.Disconnect(_cecilia.Id, Now.AddSeconds(1));

        Assert.That(_room.Tick(Now.AddSeconds(1)), Is.True);
        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Playing));
    }

    [Test]
    public void SubmitTrack_WhilePaused_WaitsForTheHost()
    {
        Start();
        _room.DisconnectHost(Now);

        SubmitForAll();

        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Submitting));
        Assert.That(_room.Tick(Now.AddMinutes(5)), Is.False);

        _room.ResumeHost(_room.HostToken, Now.AddMinutes(5));

        Assert.That(_room.Tick(Now.AddMinutes(5)), Is.True);
        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Playing));
    }

    [Test]
    public void Tick_WithNoPlayerConnected_WaitsForTheDeadline()
    {
        Start();
        foreach (var player in _room.Players)
        {
            _room.Disconnect(player.Id, Now);
        }

        Assert.That(_room.Tick(Now.AddSeconds(1)), Is.False);
        Assert.That(_room.Tick(Now + GameRoom.SubmittingDuration), Is.True);
    }

    [TestCase(0)]
    [TestCase(1)]
    public void Tick_WithFewerThanTwoEntriesAtTheDeadline_SkipsTheRound(int entries)
    {
        Start();
        foreach (var player in _room.Players.Take(entries))
        {
            _room.SubmitTrack(player.Id, Track(player.ColorIndex, "Ensam"), Now);
        }

        _room.Tick(Now + GameRoom.SubmittingDuration);

        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Submitting));
        Assert.That(_room.Round, Is.EqualTo(2));
        Assert.That(_room.SkippedRound, Is.True);
        Assert.That(_room.Prompt, Is.EqualTo("Prompt 2"));
        Assert.That(_room.Entries, Is.Empty);
    }

    // Ett bidrag räcker inte för en runda, så de som är borta får tiden fram till deadline att komma tillbaka.
    [Test]
    public void SubmitTrack_AsTheOnlyConnectedPlayer_WaitsForTheDeadline()
    {
        Start();
        _room.Disconnect(_bertil.Id, Now);
        _room.Disconnect(_cecilia.Id, Now);

        _room.SubmitTrack(_anna.Id, Track(1, "Ett"), Now);

        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Submitting));
        Assert.That(_room.Tick(Now.AddSeconds(1)), Is.False);

        _room.Rejoin(_bertil.Token);
        _room.SubmitTrack(_bertil.Id, Track(2, "Två"), Now.AddSeconds(10));

        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Playing));
        Assert.That(_room.Round, Is.EqualTo(1));
    }

    [Test]
    public void SkippedRound_IsClearedWhenTheRoundMovesOn()
    {
        Start();
        _room.Tick(Now + GameRoom.SubmittingDuration);

        _room.SubmitTrack(_anna.Id, Track(1, "Ett"), Now);
        _room.SubmitTrack(_bertil.Id, Track(2, "Två"), Now);
        _room.SubmitTrack(_cecilia.Id, Track(3, "Tre"), Now);

        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Playing));
        Assert.That(_room.SkippedRound, Is.False);
    }

    [Test]
    public void Tick_WhenTheLastRoundIsSkipped_EndsTheGame()
    {
        _room.UpdateSettings(_room.HostToken, 3, 15);
        Start();

        var now = Now;
        for (var round = 1; round <= 3; round++)
        {
            now += GameRoom.SubmittingDuration;
            Assert.That(_room.Tick(now), Is.True);
        }

        Assert.That(_room.Phase, Is.EqualTo(GamePhase.GameOver));
        Assert.That(_room.Round, Is.EqualTo(3));
        Assert.That(_room.SkippedRound, Is.False);
    }

    [Test]
    public void NextRound_StartsWithoutEntries()
    {
        Start();
        SubmitForAll();

        var now = Now;
        while (_room.Phase != GamePhase.Submitting)
        {
            now += _room.RemainingTime(now)!.Value;
            _room.Tick(now);
        }

        Assert.That(_room.Round, Is.EqualTo(2));
        Assert.That(_room.Entries, Is.Empty);
        Assert.That(_room.HasSubmitted(_anna.Id), Is.False);
        Assert.That(_room.SkippedRound, Is.False);
    }

    private void Start() =>
        _room.StartGame(_room.HostToken, Enumerable.Range(1, _room.Settings.Rounds).Select(round => $"Prompt {round}").ToList(), Now);

    private void SubmitForAll()
    {
        foreach (var player in _room.Players)
        {
            _room.SubmitTrack(player.Id, Track(player.ColorIndex, $"Låt {player.ColorIndex}"), Now);
        }
    }

    private static SubmittedTrack Track(long id, string title) => TestTrack.Of(id, title);
}
