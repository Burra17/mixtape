using Mixtape.Domain.Rooms;

namespace Mixtape.Tests.Domain;

public sealed class GameRoomEndTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 29, 21, 0, 0, TimeSpan.Zero);

    private GameRoom _room = null!;
    private DateTimeOffset _now;

    [SetUp]
    public void SetUp()
    {
        _room = GameRoom.Create(RoomCode.Parse("BCDF")!, new Random(17));
        _now = Now;
        for (var i = 1; i <= 3; i++)
        {
            _room.Join($"Spelare {i}");
        }

        Start();
    }

    [Test]
    public void End_DuringTheGame_GoesToGameOver()
    {
        var before = _room.Revision;

        var result = _room.End(_room.HostToken, _now);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(_room.Phase, Is.EqualTo(GamePhase.GameOver));
        Assert.That(_room.RemainingTime(_now), Is.Null);
        Assert.That(_room.Revision, Is.GreaterThan(before));
    }

    [Test]
    public void End_DuringVoting_DoesNotCountTheVotes()
    {
        StartVoting();
        // En röst räcker för att den ska finnas, men rundan väntar på de andra.
        var anna = _room.Players[0];
        _room.Vote(anna.Id, _room.EntryNumberOf(anna.Id) == 1 ? 2 : 1, _now);
        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Voting));

        _room.End(_room.HostToken, _now);

        Assert.That(_room.Players.Select(player => player.Score), Is.All.EqualTo(0));
        Assert.That(_room.Players.Select(player => _room.RoundPointsOf(player.Id)), Is.All.EqualTo(0));
    }

    [Test]
    public void End_AfterAFinishedRound_KeepsItsPoints()
    {
        StartVoting();
        VoteForAll();
        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Revealing));
        Tick();
        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Submitting));

        _room.End(_room.HostToken, _now);

        Assert.That(_room.Players.Sum(player => player.Score), Is.EqualTo(3));
    }

    [Test]
    public void End_DuringRevealing_KeepsTheRoundPoints()
    {
        StartVoting();
        VoteForAll();

        _room.End(_room.HostToken, _now);

        Assert.That(_room.Players.Sum(player => player.Score), Is.EqualTo(3));
        Assert.That(_room.Players.Sum(player => _room.RoundPointsOf(player.Id)), Is.EqualTo(3));
    }

    [Test]
    public void End_WhilePaused_LetsTheNextGameRunItsClock()
    {
        _room.Pause(_room.HostToken, _now);

        _room.End(_room.HostToken, _now);
        _room.PlayAgain(_room.HostToken);
        Start();

        Assert.That(_room.IsPaused, Is.False);
        Assert.That(_room.PauseReason, Is.Null);
        Assert.That(_room.Tick(_now + _room.Settings.SubmittingDuration), Is.True);
    }

    [Test]
    public void End_InTheLobby_ReturnsWrongPhase()
    {
        _room.End(_room.HostToken, _now);
        _room.PlayAgain(_room.HostToken);

        Assert.That(_room.End(_room.HostToken, _now).Error, Is.EqualTo(RoomErrors.WrongPhase));
        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Lobby));
    }

    [Test]
    public void End_AtGameOver_ReturnsWrongPhase()
    {
        _room.End(_room.HostToken, _now);

        Assert.That(_room.End(_room.HostToken, _now).Error, Is.EqualTo(RoomErrors.WrongPhase));
    }

    [TestCase(null)]
    [TestCase("fel")]
    public void End_WithWrongHostToken_ReturnsRoomNotFound(string? token)
    {
        Assert.That(_room.End(token, _now).Error, Is.EqualTo(RoomErrors.NotFound));
        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Submitting));
    }

    private void Start() =>
        _room.StartGame(_room.HostToken, Enumerable.Range(1, _room.Settings.Rounds).Select(round => $"Prompt {round}").ToList(), _now);

    // Alla tre skickar in och klippen spelas klart, så att rummet röstar.
    private void StartVoting()
    {
        foreach (var player in _room.Players)
        {
            _room.SubmitTrack(player.Id, TestTrack.Of(player.ColorIndex, $"Låt {player.ColorIndex}"), _now);
        }

        while (_room.Phase == GamePhase.Playing)
        {
            Tick();
        }
    }

    // Var och en röstar på ett bidrag som inte är det egna, så att rundan ger 3 poäng totalt. När alla röstat börjar avslöjandet direkt.
    private void VoteForAll()
    {
        foreach (var player in _room.Players)
        {
            _room.Vote(player.Id, _room.EntryNumberOf(player.Id) == 1 ? 2 : 1, _now);
        }
    }

    // Spolar fram till fasens deadline och låter klockan byta fas.
    private void Tick()
    {
        _now += _room.RemainingTime(_now) ?? TimeSpan.Zero;
        _room.Tick(_now);
    }
}
