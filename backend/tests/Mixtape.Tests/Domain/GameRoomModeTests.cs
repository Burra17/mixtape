using Mixtape.Domain.Games;
using Mixtape.Domain.Games.AuxCord;
using Mixtape.Domain.Games.DropTheNeedle;
using Mixtape.Domain.Rooms;

namespace Mixtape.Tests.Domain;

public sealed class GameRoomModeTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 20, 0, 0, TimeSpan.Zero);

    private GameRoom _room = null!;

    [SetUp]
    public void SetUp() => _room = GameRoom.Create(RoomCode.Parse("BCDF")!);

    [Test]
    public void Create_StartsWithAuxCordBattlesAndItsDefaults()
    {
        Assert.That(_room.Settings, Is.EqualTo(AuxCordSettings.Default));
        Assert.That(_room.Settings.Mode, Is.EqualTo(GameMode.AuxCord));
    }

    [Test]
    public void ChooseMode_ToDropTheNeedle_GivesItsDefaults()
    {
        var revision = _room.Revision;

        Assert.That(_room.ChooseMode(_room.HostToken, GameMode.DropTheNeedle).IsSuccess, Is.True);
        Assert.That(_room.Settings, Is.EqualTo(DropTheNeedleSettings.Default));
        Assert.That(_room.Revision, Is.EqualTo(revision + 1));
    }

    [Test]
    public void ChooseMode_TheSameMode_KeepsTheRevision()
    {
        var revision = _room.Revision;

        Assert.That(_room.ChooseMode(_room.HostToken, GameMode.AuxCord).IsSuccess, Is.True);
        Assert.That(_room.Revision, Is.EqualTo(revision));
    }

    [Test]
    public void ChooseMode_BackAndForth_RemembersTheSettingsOfEachMode()
    {
        _room.UpdateSettings<AuxCordSettings>(_room.HostToken, settings => settings.With(8, null, 120));
        _room.ChooseMode(_room.HostToken, GameMode.DropTheNeedle);
        _room.UpdateSettings<DropTheNeedleSettings>(_room.HostToken, settings => settings.With(5, 20));

        _room.ChooseMode(_room.HostToken, GameMode.AuxCord);
        Assert.That(_room.Settings, Is.EqualTo(AuxCordSettings.Create(8, 15, 120).Value));

        _room.ChooseMode(_room.HostToken, GameMode.DropTheNeedle);
        Assert.That(_room.Settings, Is.EqualTo(DropTheNeedleSettings.Create(5, 20).Value));
    }

    [TestCase(null)]
    [TestCase("fel")]
    public void ChooseMode_WithWrongHostToken_ReturnsRoomNotFound(string? token)
    {
        Assert.That(_room.ChooseMode(token, GameMode.DropTheNeedle).Error, Is.EqualTo(RoomErrors.NotFound));
        Assert.That(_room.Settings.Mode, Is.EqualTo(GameMode.AuxCord));
    }

    [Test]
    public void ChooseMode_WithAModeThatDoesNotExist_ReturnsInvalidSettings()
    {
        Assert.That(_room.ChooseMode(_room.HostToken, (GameMode)99).Error, Is.EqualTo(RoomErrors.InvalidSettings));
    }

    [Test]
    public void ChooseMode_DuringTheGame_ReturnsAlreadyStarted()
    {
        JoinPlayers(3);
        _room.StartGame(_room.HostToken, ["A", "B", "C", "D", "E"], Now);

        Assert.That(_room.ChooseMode(_room.HostToken, GameMode.DropTheNeedle).Error, Is.EqualTo(RoomErrors.AlreadyStarted));
    }

    [Test]
    public void UpdateSettings_ForAnotherModeThanTheChosen_ReturnsInvalidSettings()
    {
        var result = _room.UpdateSettings<DropTheNeedleSettings>(_room.HostToken, settings => settings.With(5, null));

        Assert.That(result.Error, Is.EqualTo(RoomErrors.InvalidSettings));
        Assert.That(_room.Settings, Is.EqualTo(AuxCordSettings.Default));
    }

    [TestCase(GameMode.AuxCord, 2, false)]
    [TestCase(GameMode.AuxCord, 3, true)]
    [TestCase(GameMode.DropTheNeedle, 1, false)]
    [TestCase(GameMode.DropTheNeedle, 2, true)]
    public void CanStart_FollowsTheChosenModesMinimum(GameMode mode, int players, bool canStart)
    {
        _room.ChooseMode(_room.HostToken, mode);
        JoinPlayers(players);

        Assert.That(_room.CanStart, Is.EqualTo(canStart));
    }

    [Test]
    public void StartGame_WhenTheGameCannotBeCreated_StaysInTheLobby()
    {
        JoinPlayers(2);
        _room.ChooseMode(_room.HostToken, GameMode.DropTheNeedle);

        var result = _room.StartGame(_room.HostToken, _ => RoomErrors.ModeNotReady, Now);

        Assert.That(result.Error, Is.EqualTo(RoomErrors.ModeNotReady));
        Assert.That(_room.Stage, Is.EqualTo(RoomStage.Lobby));
    }

    [Test]
    public void StartGame_WithWrongHostToken_DoesNotCreateTheGame()
    {
        JoinPlayers(3);
        var created = false;

        _room.StartGame("fel", _ => { created = true; return RoomErrors.ModeNotReady; }, Now);

        Assert.That(created, Is.False);
    }

    [Test]
    public void PlayAgain_KeepsTheModeAndItsSettings()
    {
        JoinPlayers(3);
        _room.UpdateSettings<AuxCordSettings>(_room.HostToken, settings => settings.With(3, null, null));
        _room.StartGame(_room.HostToken, ["A", "B", "C"], Now);
        _room.End(_room.HostToken, Now);

        _room.PlayAgain(_room.HostToken);

        Assert.That(_room.Settings, Is.EqualTo(AuxCordSettings.Create(3, 15, 90).Value));
    }

    private void JoinPlayers(int count)
    {
        foreach (var name in new[] { "Anna", "Bertil", "Cecilia", "David" }.Take(count))
        {
            _room.Join(name);
        }
    }
}
