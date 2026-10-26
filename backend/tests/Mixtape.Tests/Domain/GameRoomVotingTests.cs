using Mixtape.Domain.Rooms;

namespace Mixtape.Tests.Domain;

public sealed class GameRoomVotingTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 20, 0, 0, TimeSpan.Zero);

    private GameRoom _room = null!;
    private Player _anna = null!;
    private Player _bertil = null!;
    private Player _cecilia = null!;
    private DateTimeOffset _now;

    [SetUp]
    public void SetUp()
    {
        _room = GameRoom.Create(RoomCode.Parse("BCDF")!, new Random(17));
        _now = Now;
        _anna = _room.Join("Anna").Value;
        _bertil = _room.Join("Bertil").Value;
        _cecilia = _room.Join("Cecilia").Value;
        _room.StartGame(_room.HostToken, Enumerable.Range(1, _room.Settings.Rounds).Select(round => $"Prompt {round}").ToList(), _now);
    }

    [Test]
    public void Vote_OnAnotherEntry_LocksTheVote()
    {
        StartVoting();

        var result = _room.Vote(_anna.Id, OtherThanOwn(_anna), _now);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(_room.HasVoted(_anna.Id), Is.True);
        Assert.That(_room.VoteOf(_anna.Id), Is.EqualTo(OtherThanOwn(_anna)));
    }

    [Test]
    public void Vote_OnOwnEntry_ReturnsOwnEntry()
    {
        StartVoting();

        var result = _room.Vote(_anna.Id, _room.EntryNumberOf(_anna.Id)!.Value, _now);

        Assert.That(result.Error, Is.EqualTo(VoteErrors.OwnEntry));
        Assert.That(_room.HasVoted(_anna.Id), Is.False);
    }

    [Test]
    public void Vote_Twice_ReturnsAlreadyVoted()
    {
        StartVoting();
        var first = OtherThanOwn(_anna);
        _room.Vote(_anna.Id, first, _now);

        var second = Enumerable.Range(1, 3).First(entry => entry != first && entry != _room.EntryNumberOf(_anna.Id));
        var result = _room.Vote(_anna.Id, second, _now);

        Assert.That(result.Error, Is.EqualTo(VoteErrors.AlreadyVoted));
        Assert.That(_room.VoteOf(_anna.Id), Is.EqualTo(first));
    }

    [TestCase(0)]
    [TestCase(4)]
    public void Vote_OnANumberThatDoesNotExist_ReturnsNotFound(int entry)
    {
        StartVoting();

        Assert.That(_room.Vote(_anna.Id, entry, _now).Error, Is.EqualTo(VoteErrors.NotFound));
    }

    [Test]
    public void Vote_DuringPlaying_ReturnsWrongPhase()
    {
        SubmitFor(_anna, _bertil, _cecilia);

        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Playing));
        Assert.That(_room.Vote(_anna.Id, 1, _now).Error, Is.EqualTo(RoomErrors.WrongPhase));
    }

    [Test]
    public void Vote_ByAnUnknownPlayer_ReturnsPlayerNotFound()
    {
        StartVoting();

        Assert.That(_room.Vote("okänd", 1, _now).Error, Is.EqualTo(PlayerErrors.NotFound));
    }

    // Cecilia skickade aldrig in, så inskicket väntar till deadline. Hon röstar ändå.
    [Test]
    public void Vote_ByAPlayerWithoutAnEntry_IsAccepted()
    {
        SubmitFor(_anna, _bertil);
        _now += GameRoom.SubmittingDuration;
        _room.Tick(_now);
        PlayAllClips();

        Assert.That(_room.EntryNumberOf(_cecilia.Id), Is.Null);
        Assert.That(_room.Vote(_cecilia.Id, 1, _now).IsSuccess, Is.True);
    }

    [Test]
    public void Vote_WhenEveryoneHasVoted_GoesToRevealingDirectly()
    {
        StartVoting();

        VoteForAll(_anna, _bertil, _cecilia);

        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Revealing));
    }

    // Cecilia har tappat anslutningen och hindrar inte de andra.
    [Test]
    public void Vote_WhenEveryoneConnectedHasVoted_GoesToRevealingDirectly()
    {
        StartVoting();
        _room.Disconnect(_cecilia.Id, _now);

        VoteForAll(_anna, _bertil);

        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Revealing));
    }

    [Test]
    public void Vote_WhenSomeoneConnectedHasNotVoted_StaysInVoting()
    {
        StartVoting();

        VoteForAll(_anna, _bertil);

        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Voting));
    }

    // Röster tas emot under paus, men fasen går vidare först när storskärmen är tillbaka.
    [Test]
    public void Vote_DuringPause_IsAcceptedButWaitsForTheHost()
    {
        StartVoting();
        _room.DisconnectHost(_now);

        VoteForAll(_anna, _bertil, _cecilia);

        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Voting));
        Assert.That(_room.HasVoted(_cecilia.Id), Is.True);

        _room.ResumeHost(_room.HostToken, _now);
        Assert.That(_room.Tick(_now), Is.True);
        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Revealing));
    }

    [Test]
    public void Tick_AfterTheVotingDuration_GoesToRevealing()
    {
        StartVoting();

        Assert.That(_room.Tick(_now + GameRoom.VotingDuration - TimeSpan.FromMilliseconds(1)), Is.False);
        Assert.That(_room.Tick(_now + GameRoom.VotingDuration), Is.True);
        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Revealing));
    }

    [Test]
    public void StartRound_ClearsTheVotes()
    {
        StartVoting();
        VoteForAll(_anna, _bertil, _cecilia);

        _now += GameRoom.RevealingDuration;
        _room.Tick(_now);

        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Submitting));
        Assert.That(_room.HasVoted(_anna.Id), Is.False);
        Assert.That(_room.VoteOf(_anna.Id), Is.Null);
    }

    [Test]
    public void EntryNumberOf_IsThePlaceInThePlayback()
    {
        StartVoting();

        foreach (var player in new[] { _anna, _bertil, _cecilia })
        {
            var number = _room.EntryNumberOf(player.Id)!.Value;
            Assert.That(_room.Entries[number - 1].PlayerId, Is.EqualTo(player.Id));
        }
    }

    [Test]
    public void CanVote_OutsideVoting_ReturnsWrongPhase()
    {
        Assert.That(_room.CanVote().Error, Is.EqualTo(RoomErrors.WrongPhase));

        StartVoting();

        Assert.That(_room.CanVote().IsSuccess, Is.True);
    }

    // Alla tre skickar in och klippen spelas klart, så att rummet röstar.
    private void StartVoting()
    {
        SubmitFor(_anna, _bertil, _cecilia);
        PlayAllClips();
    }

    private void SubmitFor(params Player[] players)
    {
        foreach (var player in players)
        {
            _room.SubmitTrack(player.Id, TestTrack.Of(player.ColorIndex, $"Låt {player.ColorIndex}"), _now);
        }
    }

    private void PlayAllClips()
    {
        while (_room.Phase == GamePhase.Playing)
        {
            _now += _room.Settings.ClipLength;
            _room.Tick(_now);
        }

        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Voting));
    }

    private void VoteForAll(params Player[] players)
    {
        foreach (var player in players)
        {
            _room.Vote(player.Id, OtherThanOwn(player), _now);
        }
    }

    // Ett bidrag som inte är spelarens eget.
    private int OtherThanOwn(Player player) => _room.EntryNumberOf(player.Id) == 1 ? 2 : 1;
}
