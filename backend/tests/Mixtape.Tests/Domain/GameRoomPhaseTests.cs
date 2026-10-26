using Mixtape.Domain.Rooms;

namespace Mixtape.Tests.Domain;

public sealed class GameRoomPhaseTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 20, 20, 0, 0, TimeSpan.Zero);

    private GameRoom _room = null!;
    private DateTimeOffset _now;

    [SetUp]
    public void SetUp()
    {
        _room = GameRoom.Create(RoomCode.Parse("BCDF")!, Random.Shared);
        _now = Now;
    }

    [Test]
    public void StartGame_WithThreePlayers_StartsTheFirstRound()
    {
        JoinPlayers(3);

        var result = _room.StartGame(_room.HostToken, Prompts(), _now);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Submitting));
        Assert.That(_room.Round, Is.EqualTo(1));
        Assert.That(_room.RemainingTime(_now), Is.EqualTo(GameSettings.Default.SubmittingDuration));
    }

    [Test]
    public void StartGame_WithTwoPlayers_ReturnsNotEnoughPlayers()
    {
        JoinPlayers(2);

        Assert.That(_room.StartGame(_room.HostToken, Prompts(), _now).Error, Is.EqualTo(RoomErrors.NotEnoughPlayers));
        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Lobby));
    }

    [Test]
    public void StartGame_CountsPlayersThatAreAway()
    {
        var anna = _room.Join("Anna").Value;
        JoinPlayers(2);
        _room.Disconnect(anna.Id, _now);

        Assert.That(_room.StartGame(_room.HostToken, Prompts(), _now).IsSuccess, Is.True);
    }

    [TestCase(null)]
    [TestCase("fel")]
    public void StartGame_WithWrongHostToken_ReturnsRoomNotFound(string? token)
    {
        JoinPlayers(3);

        Assert.That(_room.StartGame(token, Prompts(), _now).Error, Is.EqualTo(RoomErrors.NotFound));
        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Lobby));
    }

    [Test]
    public void StartGame_Twice_ReturnsAlreadyStarted()
    {
        StartWithPlayers(3);

        Assert.That(_room.StartGame(_room.HostToken, Prompts(), _now).Error, Is.EqualTo(RoomErrors.AlreadyStarted));
        Assert.That(_room.Round, Is.EqualTo(1));
    }

    [Test]
    public void Tick_BeforeTheDeadline_KeepsThePhase()
    {
        StartWithPlayers(3);

        Assert.That(_room.Tick(_now + GameSettings.Default.SubmittingDuration - TimeSpan.FromMilliseconds(1)), Is.False);
        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Submitting));
    }

    [Test]
    public void Tick_InTheLobby_DoesNothing()
    {
        JoinPlayers(3);

        Assert.That(_room.Tick(_now.AddHours(1)), Is.False);
        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Lobby));
    }

    [Test]
    public void Tick_GoesThroughThePhasesOfARoundWithTheirDurations()
    {
        StartWithPlayers(3);
        SubmitTwoEntries();
        var clip = GameSettings.Default.ClipLength;

        AdvanceToNextPhase(GameSettings.Default.SubmittingDuration);
        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Playing));
        Assert.That(_room.RemainingTime(_now), Is.EqualTo(clip));

        AdvanceToNextPhase(clip);
        Assert.That(_room.ClipNumber, Is.EqualTo(2));
        Assert.That(_room.RemainingTime(_now), Is.EqualTo(clip));

        AdvanceToNextPhase(clip);
        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Voting));
        Assert.That(_room.RemainingTime(_now), Is.EqualTo(GameRoom.VotingDuration));

        AdvanceToNextPhase(GameRoom.VotingDuration);
        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Revealing));
        Assert.That(_room.RemainingTime(_now), Is.EqualTo(GameRoom.RevealingDuration));

        AdvanceToNextPhase(GameRoom.RevealingDuration);
        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Submitting));
        Assert.That(_room.Round, Is.EqualTo(2));
    }

    [TestCase(3)]
    [TestCase(5)]
    [TestCase(8)]
    public void Tick_AfterTheLastRound_EndsTheGame(int rounds)
    {
        JoinPlayers(3);
        _room.UpdateSettings(_room.HostToken, rounds, 15, null);
        _room.StartGame(_room.HostToken, Prompts(), _now);

        var phaseChanges = 0;
        while (_room.Phase != GamePhase.GameOver && phaseChanges < 100)
        {
            if (_room.Phase == GamePhase.Submitting)
            {
                SubmitTwoEntries();
            }

            AdvanceToNextPhase(_room.RemainingTime(_now)!.Value);
            phaseChanges++;
        }

        Assert.That(_room.Phase, Is.EqualTo(GamePhase.GameOver));
        // Inskick, två klipp, röstning och avslöjande i varje runda.
        Assert.That(phaseChanges, Is.EqualTo(rounds * 5));
        Assert.That(_room.Round, Is.EqualTo(rounds));
        Assert.That(_room.RemainingTime(_now), Is.Null);
        Assert.That(_room.Tick(_now.AddHours(1)), Is.False);
    }

    [Test]
    public void Tick_IncreasesTheRevisionOnlyWhenThePhaseChanges()
    {
        StartWithPlayers(3);
        var started = _room.Revision;

        _room.Tick(_now.AddSeconds(1));
        Assert.That(_room.Revision, Is.EqualTo(started));

        _room.Tick(_now + GameSettings.Default.SubmittingDuration);
        Assert.That(_room.Revision, Is.EqualTo(started + 1));
    }

    [Test]
    public void DisconnectHost_FreezesThePhaseUntilTheHostIsBack()
    {
        StartWithPlayers(3);
        SubmitTwoEntries();
        _room.DisconnectHost(_now.AddSeconds(33));

        Assert.That(_room.Tick(_now.AddMinutes(10)), Is.False);
        Assert.That(_room.RemainingTime(_now.AddMinutes(10)), Is.EqualTo(GameSettings.Default.SubmittingDuration - TimeSpan.FromSeconds(33)));

        var back = _now.AddMinutes(10);
        _room.ResumeHost(_room.HostToken, back);

        Assert.That(_room.Tick(back + GameSettings.Default.SubmittingDuration - TimeSpan.FromSeconds(34)), Is.False);
        Assert.That(_room.Tick(back + GameSettings.Default.SubmittingDuration - TimeSpan.FromSeconds(33)), Is.True);
        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Playing));
    }

    [Test]
    public void PauseInTheLobby_LeavesNoClockAndTheFirstPhaseGetsItsWholeTime()
    {
        JoinPlayers(3);
        _room.DisconnectHost(_now);
        var back = _now.AddMinutes(10);
        _room.ResumeHost(_room.HostToken, back);

        Assert.That(_room.RemainingTime(back), Is.Null);

        _room.StartGame(_room.HostToken, Prompts(), back);
        Assert.That(_room.RemainingTime(back), Is.EqualTo(GameSettings.Default.SubmittingDuration));
    }

    [Test]
    public void UpdateSettings_AsHostInTheLobby_ChangesTheSettings()
    {
        var result = _room.UpdateSettings(_room.HostToken, 8, 20, 120);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(_room.Settings.Rounds, Is.EqualTo(8));
        Assert.That(_room.Settings.ClipSeconds, Is.EqualTo(20));
        Assert.That(_room.Settings.SubmittingSeconds, Is.EqualTo(120));
    }

    [TestCase(60)]
    [TestCase(120)]
    public void StartGame_GivesTheSubmittingPhaseTheChosenTime(int submittingSeconds)
    {
        JoinPlayers(3);
        _room.UpdateSettings(_room.HostToken, null, null, submittingSeconds);

        _room.StartGame(_room.HostToken, Prompts(), _now);

        Assert.That(_room.RemainingTime(_now), Is.EqualTo(TimeSpan.FromSeconds(submittingSeconds)));
        Assert.That(_room.Tick(_now + TimeSpan.FromSeconds(submittingSeconds) - TimeSpan.FromMilliseconds(1)), Is.False);
        Assert.That(_room.Tick(_now + TimeSpan.FromSeconds(submittingSeconds)), Is.True);
    }

    [Test]
    public void UpdateSettings_OneAtATime_KeepsTheOthers()
    {
        _room.UpdateSettings(_room.HostToken, 3, null, null);
        _room.UpdateSettings(_room.HostToken, null, 10, null);
        _room.UpdateSettings(_room.HostToken, null, null, 60);

        Assert.That(_room.Settings.Rounds, Is.EqualTo(3));
        Assert.That(_room.Settings.ClipSeconds, Is.EqualTo(10));
        Assert.That(_room.Settings.SubmittingSeconds, Is.EqualTo(60));
    }

    [Test]
    public void UpdateSettings_WithWrongHostToken_ReturnsRoomNotFound()
    {
        Assert.That(_room.UpdateSettings("fel", 8, 20, null).Error, Is.EqualTo(RoomErrors.NotFound));
        Assert.That(_room.Settings, Is.EqualTo(GameSettings.Default));
    }

    [Test]
    public void UpdateSettings_WithValueThatIsNotAnOption_ReturnsInvalidSettings()
    {
        Assert.That(_room.UpdateSettings(_room.HostToken, 4, 15, null).Error, Is.EqualTo(RoomErrors.InvalidSettings));
        Assert.That(_room.Settings, Is.EqualTo(GameSettings.Default));
    }

    [Test]
    public void UpdateSettings_DuringTheGame_ReturnsAlreadyStarted()
    {
        StartWithPlayers(3);

        Assert.That(_room.UpdateSettings(_room.HostToken, 8, 20, null).Error, Is.EqualTo(RoomErrors.AlreadyStarted));
        Assert.That(_room.Settings, Is.EqualTo(GameSettings.Default));
    }

    [Test]
    public void UpdateSettings_WithTheSameValues_KeepsTheRevision()
    {
        var before = _room.Revision;

        _room.UpdateSettings(_room.HostToken, 5, 15, null);

        Assert.That(_room.Revision, Is.EqualTo(before));
    }

    [Test]
    public void Join_DuringTheGame_ReturnsAlreadyStarted()
    {
        StartWithPlayers(3);

        Assert.That(_room.Join("Sen").Error, Is.EqualTo(RoomErrors.AlreadyStarted));
        Assert.That(_room.Players, Has.Count.EqualTo(3));
    }

    [Test]
    public void Leave_DuringTheGame_ReturnsAlreadyStarted()
    {
        StartWithPlayers(3);

        Assert.That(_room.Leave(_room.Players[0].Token, _now).Error, Is.EqualTo(RoomErrors.AlreadyStarted));
        Assert.That(_room.Players, Has.Count.EqualTo(3));
    }

    [Test]
    public void RemovePlayer_DuringTheGame_ReturnsAlreadyStarted()
    {
        StartWithPlayers(3);

        Assert.That(_room.RemovePlayer(_room.HostToken, _room.Players[0].Id).Error, Is.EqualTo(RoomErrors.AlreadyStarted));
        Assert.That(_room.Players, Has.Count.EqualTo(3));
    }

    [Test]
    public void Rejoin_DuringTheGame_KeepsThePlace()
    {
        StartWithPlayers(3);
        var anna = _room.Players[0];
        _room.Disconnect(anna.Id, _now);

        Assert.That(_room.Rejoin(anna.Token).Value, Is.SameAs(anna));
        Assert.That(anna.IsConnected, Is.True);
    }

    // En prompt per runda, så som servern drar dem.
    private List<string> Prompts() => Enumerable.Range(1, _room.Settings.Rounds).Select(round => $"Prompt {round}").ToList();

    private void JoinPlayers(int count)
    {
        for (var i = 1; i <= count; i++)
        {
            _room.Join($"Spelare {_room.Players.Count + 1}");
        }
    }

    private void StartWithPlayers(int count)
    {
        JoinPlayers(count);
        _room.StartGame(_room.HostToken, Prompts(), _now);
    }

    // Två bidrag räcker för att rundan inte hoppas över, men inte för att alla tre ska vara klara.
    private void SubmitTwoEntries()
    {
        foreach (var player in _room.Players.Take(2))
        {
            _room.SubmitTrack(player.Id, TestTrack.Of(player.ColorIndex, $"Låt {player.ColorIndex}", "Artist"), _now);
        }
    }

    // Spolar fram till fasens deadline och låter klockan byta fas.
    private void AdvanceToNextPhase(TimeSpan duration)
    {
        _now += duration;
        Assert.That(_room.Tick(_now), Is.True);
    }
}
