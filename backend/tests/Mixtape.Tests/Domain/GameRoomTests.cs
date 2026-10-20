using Mixtape.Domain.Rooms;

namespace Mixtape.Tests.Domain;

public sealed class GameRoomTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 20, 20, 0, 0, TimeSpan.Zero);

    private GameRoom _room = null!;

    [SetUp]
    public void SetUp()
    {
        _room = GameRoom.Create(RoomCode.Parse("BCDF")!);
    }

    [Test]
    public void Join_AddsPlayerWithTrimmedName()
    {
        var result = _room.Join("  Anna ");

        Assert.That(result.Value.Name.Value, Is.EqualTo("Anna"));
        Assert.That(_room.Players, Is.EqualTo(new[] { result.Value }));
    }

    [Test]
    public void Join_GivesEachPlayerSecretTokenAndPublicId()
    {
        var anna = _room.Join("Anna").Value;
        var bertil = _room.Join("Bertil").Value;

        Assert.That(anna.Token, Is.Not.EqualTo(bertil.Token));
        Assert.That(anna.Id, Is.Not.EqualTo(bertil.Id));
        Assert.That(anna.Id, Is.Not.EqualTo(anna.Token));
    }

    [Test]
    public void Join_UpToEightPlayers_GivesEachTheNextColor()
    {
        var colors = Enumerable.Range(1, GameRoom.MaxPlayers).Select(i => _room.Join($"Spelare {i}").Value.ColorIndex);

        Assert.That(colors, Is.EqualTo(Enumerable.Range(1, GameRoom.MaxPlayers)));
    }

    [Test]
    public void Join_NinthPlayer_ReturnsFull()
    {
        for (var i = 1; i <= GameRoom.MaxPlayers; i++)
        {
            _room.Join($"Spelare {i}");
        }

        var result = _room.Join("Nionde");

        Assert.That(result.Error, Is.EqualTo(RoomErrors.Full));
        Assert.That(_room.Players, Has.Count.EqualTo(GameRoom.MaxPlayers));
    }

    [TestCase("Anna")]
    [TestCase("ANNA")]
    [TestCase(" anna ")]
    public void Join_WithTakenName_ReturnsNameTaken(string name)
    {
        _room.Join("Anna");

        Assert.That(_room.Join(name).Error, Is.EqualTo(PlayerErrors.NameTaken));
    }

    [Test]
    public void Join_WithInvalidName_ReturnsInvalidName()
    {
        Assert.That(_room.Join("   ").Error, Is.EqualTo(PlayerErrors.InvalidName));
        Assert.That(_room.Players, Is.Empty);
    }

    [Test]
    public void Join_AfterSomeoneLeft_GivesTheFreedColor()
    {
        _room.Join("Anna");
        var bertil = _room.Join("Bertil").Value;
        _room.Join("Cecilia");
        _room.Leave(bertil.Token, Now);

        Assert.That(_room.Join("David").Value.ColorIndex, Is.EqualTo(bertil.ColorIndex));
    }

    [Test]
    public void Leave_WithOwnToken_RemovesThePlayer()
    {
        var anna = _room.Join("Anna").Value;

        var result = _room.Leave(anna.Token, Now);

        Assert.That(result.Value, Is.SameAs(anna));
        Assert.That(_room.Players, Is.Empty);
    }

    [TestCase(null)]
    [TestCase("fel")]
    public void Leave_WithUnknownToken_ReturnsNotFound(string? token)
    {
        _room.Join("Anna");

        Assert.That(_room.Leave(token, Now).Error, Is.EqualTo(PlayerErrors.NotFound));
        Assert.That(_room.Players, Has.Count.EqualTo(1));
    }

    [Test]
    public void Leave_FreesTheName()
    {
        var anna = _room.Join("Anna").Value;
        _room.Leave(anna.Token, Now);

        Assert.That(_room.Join("Anna").IsSuccess, Is.True);
    }

    [Test]
    public void RemovePlayer_AsHost_RemovesThePlayer()
    {
        var anna = _room.Join("Anna").Value;

        var result = _room.RemovePlayer(_room.HostToken, anna.Id);

        Assert.That(result.Value, Is.SameAs(anna));
        Assert.That(_room.Players, Is.Empty);
    }

    [Test]
    public void RemovePlayer_WithWrongHostToken_ReturnsRoomNotFound()
    {
        var anna = _room.Join("Anna").Value;

        Assert.That(_room.RemovePlayer(anna.Token, anna.Id).Error, Is.EqualTo(RoomErrors.NotFound));
        Assert.That(_room.Players, Has.Count.EqualTo(1));
    }

    [Test]
    public void RemovePlayer_UnknownPlayer_ReturnsNotFound()
    {
        Assert.That(_room.RemovePlayer(_room.HostToken, "okänd").Error, Is.EqualTo(PlayerErrors.NotFound));
    }

    [TestCase(2, false)]
    [TestCase(3, true)]
    public void CanStart_NeedsThreePlayers(int players, bool expected)
    {
        for (var i = 1; i <= players; i++)
        {
            _room.Join($"Spelare {i}");
        }

        Assert.That(_room.CanStart, Is.EqualTo(expected));
    }

    [Test]
    public void Revision_IncreasesOnEveryChangeButNotOnRejection()
    {
        var anna = _room.Join("Anna").Value;
        _room.Join("Anna");
        _room.Leave(anna.Token, Now);

        Assert.That(_room.Revision, Is.EqualTo(2));
    }
}
