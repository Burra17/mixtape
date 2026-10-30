using Mixtape.Application.Rooms;
using Mixtape.Domain.Games.AuxCord;
using Mixtape.Domain.Rooms;

namespace Mixtape.Tests.Domain;

public sealed class GameRoomRevealingTests
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
        _room = AuxCordRoom.Create(RoomCode.Parse("BCDF")!, new Random(17));
        _now = Now;
        _anna = _room.Join("Anna").Value;
        _bertil = _room.Join("Bertil").Value;
        _cecilia = _room.Join("Cecilia").Value;
        _room.StartGame(_room.HostToken, Enumerable.Range(1, _room.Settings.Rounds).Select(round => $"Prompt {round}").ToList(), _now);
    }

    [Test]
    public void Revealing_GivesOnePointPerVoteToTheOwnerOfTheEntry()
    {
        StartVoting(_anna, _bertil, _cecilia);

        VoteFor(_anna, _cecilia);
        VoteFor(_bertil, _cecilia);
        VoteFor(_cecilia, _anna);

        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Revealing));
        Assert.That(_cecilia.Score, Is.EqualTo(2));
        Assert.That(_anna.Score, Is.EqualTo(1));
        Assert.That(_bertil.Score, Is.Zero);
        Assert.That(_room.RoundPointsOf(_cecilia.Id), Is.EqualTo(2));
        Assert.That(_room.VotesFor(_room.EntryNumberOf(_cecilia.Id)!.Value), Is.EqualTo(2));
    }

    // Cecilia skickade aldrig in, men hennes röst ger ändå en poäng. Själv får hon inga.
    [Test]
    public void Revealing_CountsTheVoteOfAPlayerWithoutAnEntry()
    {
        StartVoting(_anna, _bertil);

        VoteFor(_anna, _bertil);
        VoteFor(_bertil, _anna);
        VoteFor(_cecilia, _anna);

        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Revealing));
        Assert.That(_anna.Score, Is.EqualTo(2));
        Assert.That(_cecilia.Score, Is.Zero);
        Assert.That(_room.RoundPointsOf(_cecilia.Id), Is.Zero);
    }

    [Test]
    public void Tick_AtTheVotingDeadline_AwardsThePointsThatCameIn()
    {
        StartVoting(_anna, _bertil, _cecilia);
        VoteFor(_anna, _bertil);

        _now += AuxCordGame.VotingDuration;
        _room.Tick(_now);

        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Revealing));
        Assert.That(_bertil.Score, Is.EqualTo(1));
        Assert.That(_anna.Score + _cecilia.Score, Is.Zero);
    }

    [Test]
    public void Revealing_LastsTwentySeconds()
    {
        StartVoting(_anna, _bertil, _cecilia);
        VoteFor(_anna, _bertil);
        VoteFor(_bertil, _anna);
        VoteFor(_cecilia, _anna);

        Assert.That(_room.RemainingTime(_now), Is.EqualTo(TimeSpan.FromSeconds(20)));
    }

    [Test]
    public void Score_AddsUpOverRounds_WhileTheRoundPointsStartOver()
    {
        StartVoting(_anna, _bertil, _cecilia);
        VoteFor(_anna, _cecilia);
        VoteFor(_bertil, _cecilia);
        VoteFor(_cecilia, _anna);
        _now += AuxCordGame.RevealingDuration;
        _room.Tick(_now);

        StartVoting(_anna, _bertil, _cecilia);
        VoteFor(_anna, _bertil);
        VoteFor(_bertil, _cecilia);
        VoteFor(_cecilia, _bertil);

        Assert.That(_room.Round, Is.EqualTo(2));
        Assert.That(_cecilia.Score, Is.EqualTo(3));
        Assert.That(_bertil.Score, Is.EqualTo(2));
        Assert.That(_room.RoundPointsOf(_cecilia.Id), Is.EqualTo(1));
        Assert.That(_room.RoundPointsOf(_anna.Id), Is.Zero);
    }

    // Cecilia röstar inte, så Anna och Bertil får en poäng var och delar förstaplatsen.
    [Test]
    public void Standings_WithEqualScores_SharesThePlaceAndSkipsTheNext()
    {
        StartVoting(_anna, _bertil, _cecilia);
        VoteFor(_anna, _bertil);
        VoteFor(_bertil, _anna);
        _now += AuxCordGame.VotingDuration;
        _room.Tick(_now);

        Assert.That(_room.Standings.Select(standing => (standing.Player, standing.Place)), Is.EqualTo(new[] { (_anna, 1), (_bertil, 1), (_cecilia, 3) }));
    }

    [Test]
    public void Standings_AreSortedByScore()
    {
        StartVoting(_anna, _bertil, _cecilia);
        VoteFor(_anna, _cecilia);
        VoteFor(_bertil, _cecilia);
        VoteFor(_cecilia, _bertil);

        Assert.That(_room.Standings.Select(standing => (standing.Player, standing.Place)), Is.EqualTo(new[] { (_cecilia, 1), (_bertil, 2), (_anna, 3) }));
    }

    // De som skickar in gör det, och klippen spelas klart, så att rummet röstar.
    private void StartVoting(params Player[] submitters)
    {
        foreach (var player in submitters)
        {
            _room.SubmitTrack(player.Id, TestTrack.Of(_room.Round * 10 + player.ColorIndex, $"Låt {player.ColorIndex}"), _now);
        }

        if (_room.Phase == GamePhase.Submitting)
        {
            _now += GameSettings.Default.SubmittingDuration;
            _room.Tick(_now);
        }

        while (_room.Phase == GamePhase.Playing)
        {
            _now += _room.Settings.ClipLength;
            _room.Tick(_now);
        }

        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Voting));
    }

    private void VoteFor(Player voter, Player owner) => _room.Vote(voter.Id, _room.EntryNumberOf(owner.Id)!.Value, _now);
}
