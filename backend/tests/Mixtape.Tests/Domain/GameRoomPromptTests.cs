using Mixtape.Domain.Rooms;

namespace Mixtape.Tests.Domain;

public sealed class GameRoomPromptTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 20, 0, 0, TimeSpan.Zero);

    private GameRoom _room = null!;
    private DateTimeOffset _now;

    [SetUp]
    public void SetUp()
    {
        _room = GameRoom.Create(RoomCode.Parse("BCDF")!);
        _now = Now;
        foreach (var name in new[] { "Anna", "Bertil", "Cecilia" })
        {
            _room.Join(name);
        }
    }

    [Test]
    public void Prompt_InTheLobby_IsNull()
    {
        Assert.That(_room.Prompt, Is.Null);
    }

    [Test]
    public void Prompt_FollowsTheRoundThroughAllItsPhasesAndIsNullAfterTheGame()
    {
        _room.UpdateSettings(_room.HostToken, 8, null);
        var prompts = Enumerable.Range(1, 8).Select(round => $"Prompt {round}").ToList();
        _room.StartGame(_room.HostToken, prompts, _now);

        var seen = new List<string?>();
        while (_room.Phase != GamePhase.GameOver)
        {
            seen.Add(_room.Prompt);
            if (_room.Phase == GamePhase.Submitting)
            {
                SubmitTwoEntries();
            }

            _now += _room.RemainingTime(_now)!.Value;
            _room.Tick(_now);
        }

        // Fem steg per runda (inskick, två klipp, röstning och avslöjande), och samma prompt i alla.
        Assert.That(seen, Is.EqualTo(prompts.SelectMany(prompt => Enumerable.Repeat(prompt, 5))));
        Assert.That(_room.Prompt, Is.Null);
    }

    [Test]
    public void StartGame_RemembersThePromptsAsUsed()
    {
        _room.StartGame(_room.HostToken, ["A", "B", "C", "D", "E"], _now);

        Assert.That(_room.UsedPrompts, Is.EquivalentTo(new[] { "A", "B", "C", "D", "E" }));
    }

    [Test]
    public void StartGame_WithWrongNumberOfPrompts_Throws()
    {
        Assert.Throws<ArgumentException>(() => _room.StartGame(_room.HostToken, ["A", "B"], _now));
        Assert.That(_room.Phase, Is.EqualTo(GamePhase.Lobby));
    }

    [Test]
    public void StartGame_WithTheSamePromptTwice_Throws()
    {
        Assert.Throws<ArgumentException>(() => _room.StartGame(_room.HostToken, ["A", "B", "C", "D", "A"], _now));
        Assert.That(_room.UsedPrompts, Is.Empty);
    }

    [Test]
    public void StartGame_RejectedByARule_DoesNotUseThePrompts()
    {
        _room.StartGame("fel", ["A", "B", "C", "D", "E"], _now);

        Assert.That(_room.UsedPrompts, Is.Empty);
    }

    // Två bidrag, så att rundan inte hoppas över.
    private void SubmitTwoEntries()
    {
        foreach (var player in _room.Players.Take(2))
        {
            _room.SubmitTrack(player.Id, TestTrack.Of(player.ColorIndex, $"Låt {player.ColorIndex}", "Artist"), _now);
        }
    }
}
