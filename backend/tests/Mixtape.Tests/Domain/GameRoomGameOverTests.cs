using Mixtape.Domain.Rooms;

namespace Mixtape.Tests.Domain;

public sealed class GameRoomGameOverTests
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
        _room.UpdateSettings(_room.HostToken, 3, 10, 120);
        _room.StartGame(_room.HostToken, ["Prompt 1", "Prompt 2", "Prompt 3"], _now);
    }

    // Anna och Bertil röstar på varandra i sista rundan, och Cecilia röstar inte.
    [Test]
    public void GameOver_WithEqualScores_SharesTheVictory()
    {
        PlayToTheEnd();

        Assert.That(_room.Phase, Is.EqualTo(GamePhase.GameOver));
        Assert.That(_room.Standings.Select(standing => (standing.Player, standing.Place)), Is.EqualTo(new[] { (_anna, 1), (_bertil, 1), (_cecilia, 3) }));
    }

    [Test]
    public void PlayAgain_ResetsTheScoresAndTheRoundAndGoesToTheLobby()
    {
        PlayToTheEnd();
        Assert.That(_room.Entries, Is.Not.Empty);
        Assert.That(_room.HasVoted(_anna.Id), Is.True);

        var result = _room.PlayAgain(_room.HostToken);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Lobby));
        Assert.That(_room.Round, Is.Zero);
        Assert.That(_room.Prompt, Is.Null);
        Assert.That(_room.Entries, Is.Empty);
        Assert.That(_room.HasVoted(_anna.Id), Is.False);
        Assert.That(_room.Players.Select(player => player.Score), Has.All.Zero);
        Assert.That(_room.RemainingTime(_now), Is.Null);
    }

    // Cecilia har tappat anslutningen men behåller sin plats, så att hon kan komma tillbaka till nästa spel.
    [Test]
    public void PlayAgain_KeepsThePlayersTheSettingsAndTheUsedPrompts()
    {
        PlayToTheEnd();
        _room.Disconnect(_cecilia.Id, _now);

        _room.PlayAgain(_room.HostToken);

        Assert.That(_room.Players, Is.EqualTo(new[] { _anna, _bertil, _cecilia }));
        Assert.That(_cecilia.IsConnected, Is.False);
        Assert.That(_room.Settings, Is.EqualTo(GameSettings.Create(3, 10, 120).Value));
        Assert.That(_room.UsedPrompts, Is.EquivalentTo(new[] { "Prompt 1", "Prompt 2", "Prompt 3" }));
    }

    [Test]
    public void PlayAgain_ThenStartGame_StartsANewGame()
    {
        PlayToTheEnd();
        _room.PlayAgain(_room.HostToken);

        var started = _room.StartGame(_room.HostToken, ["Prompt 4", "Prompt 5", "Prompt 6"], _now);

        Assert.That(started.IsSuccess, Is.True);
        Assert.That(_room.Round, Is.EqualTo(1));
        Assert.That(_room.Prompt, Is.EqualTo("Prompt 4"));
    }

    [Test]
    public void PlayAgain_BeforeTheGameIsOver_ReturnsWrongPhase()
    {
        Assert.That(_room.PlayAgain(_room.HostToken).Error, Is.EqualTo(RoomErrors.WrongPhase));
        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Submitting));
    }

    [TestCase(null)]
    [TestCase("fel")]
    public void PlayAgain_WithWrongHostToken_ReturnsRoomNotFound(string? token)
    {
        PlayToTheEnd();

        Assert.That(_room.PlayAgain(token).Error, Is.EqualTo(RoomErrors.NotFound));
        Assert.That(_room.Phase, Is.EqualTo(GamePhase.GameOver));
    }

    // De två första rundorna hoppas över och den sista spelas, så att slutställningen har bidrag och röster kvar som PlayAgain måste tömma.
    private void PlayToTheEnd()
    {
        while (_room.Round < _room.Settings.Rounds)
        {
            _now += _room.RemainingTime(_now)!.Value;
            _room.Tick(_now);
        }

        _room.SubmitTrack(_anna.Id, TestTrack.Of(1, "Ett"), _now);
        _room.SubmitTrack(_bertil.Id, TestTrack.Of(2, "Två"), _now);
        AdvanceUntil(GamePhase.Voting);
        _room.Vote(_anna.Id, _room.EntryNumberOf(_bertil.Id)!.Value, _now);
        _room.Vote(_bertil.Id, _room.EntryNumberOf(_anna.Id)!.Value, _now);
        AdvanceUntil(GamePhase.GameOver);
    }

    // Spolar fram en fas i taget tills rummet når fasen.
    private void AdvanceUntil(GamePhase phase)
    {
        while (_room.Phase != phase)
        {
            _now += _room.RemainingTime(_now) ?? throw new InvalidOperationException($"Rummet står still i {_room.Phase}.");
            _room.Tick(_now);
        }
    }
}
