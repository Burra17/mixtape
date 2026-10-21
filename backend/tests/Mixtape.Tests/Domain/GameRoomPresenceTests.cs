using Mixtape.Domain.Rooms;

namespace Mixtape.Tests.Domain;

public sealed class GameRoomPresenceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 20, 20, 0, 0, TimeSpan.Zero);

    private GameRoom _room = null!;

    [SetUp]
    public void SetUp()
    {
        _room = GameRoom.Create(RoomCode.Parse("BCDF")!);
    }

    [Test]
    public void Disconnect_KeepsThePlayerButMarksItAway()
    {
        var anna = _room.Join("Anna").Value;

        _room.Disconnect(anna.Id, Now);

        Assert.That(_room.Players, Is.EqualTo(new[] { anna }));
        Assert.That(anna.IsConnected, Is.False);
    }

    [Test]
    public void Rejoin_WithOwnToken_MarksThePlayerConnectedAgain()
    {
        var anna = _room.Join("Anna").Value;
        _room.Disconnect(anna.Id, Now);

        var result = _room.Rejoin(anna.Token);

        Assert.That(result.Value, Is.SameAs(anna));
        Assert.That(anna.IsConnected, Is.True);
    }

    [TestCase(null)]
    [TestCase("fel")]
    public void Rejoin_WithUnknownToken_ReturnsNotFound(string? token)
    {
        _room.Join("Anna");

        Assert.That(_room.Rejoin(token).Error, Is.EqualTo(PlayerErrors.NotFound));
    }

    [Test]
    public void Rejoin_AfterBeingRemoved_ReturnsNotFound()
    {
        var anna = _room.Join("Anna").Value;
        _room.RemovePlayer(_room.HostToken, anna.Id);

        Assert.That(_room.Rejoin(anna.Token).Error, Is.EqualTo(PlayerErrors.NotFound));
    }

    [Test]
    public void Disconnect_RemovedPlayer_ReturnsNotFound()
    {
        var anna = _room.Join("Anna").Value;
        _room.Leave(anna.Token, Now);

        Assert.That(_room.Disconnect(anna.Id, Now).Error, Is.EqualTo(PlayerErrors.NotFound));
    }

    [Test]
    public void Disconnect_KeepsTheColorTaken()
    {
        var anna = _room.Join("Anna").Value;
        _room.Disconnect(anna.Id, Now);

        Assert.That(_room.Join("Bertil").Value.ColorIndex, Is.Not.EqualTo(anna.ColorIndex));
    }

    [Test]
    public void DisconnectHost_PausesUntilTheHostResumes()
    {
        _room.DisconnectHost(Now);
        Assert.That(_room.IsPaused, Is.True);

        _room.ResumeHost(_room.HostToken, Now);
        Assert.That(_room.IsPaused, Is.False);
    }

    [Test]
    public void ResumeHost_WithWrongToken_ReturnsNotFoundAndStaysPaused()
    {
        _room.DisconnectHost(Now);

        Assert.That(_room.ResumeHost("fel", Now).Error, Is.EqualTo(RoomErrors.NotFound));
        Assert.That(_room.IsPaused, Is.True);
    }

    [Test]
    public void Revision_IncreasesOnlyWhenPresenceChanges()
    {
        var anna = _room.Join("Anna").Value;
        var afterJoin = _room.Revision;

        _room.Rejoin(anna.Token);
        _room.ResumeHost(_room.HostToken, Now);
        Assert.That(_room.Revision, Is.EqualTo(afterJoin));

        _room.Disconnect(anna.Id, Now);
        _room.DisconnectHost(Now);
        Assert.That(_room.Revision, Is.EqualTo(afterJoin + 2));
    }
}
