using Mixtape.Domain.Rooms;

namespace Mixtape.Tests.Domain;

public sealed class GameRoomAbandonedTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 20, 20, 0, 0, TimeSpan.Zero);

    private GameRoom _room = null!;

    [SetUp]
    public void SetUp()
    {
        _room = GameRoom.Create(RoomCode.Parse("BCDF")!);
    }

    [Test]
    public void NewRoom_IsNotEmpty()
    {
        Assert.That(_room.EmptySince, Is.Null);
    }

    [TestCase(29, false)]
    [TestCase(30, true)]
    public void IsAbandoned_AfterThirtyMinutesEmpty(int minutes, bool expected)
    {
        _room.DisconnectHost(Now);

        Assert.That(_room.IsAbandoned(Now.AddMinutes(minutes)), Is.EqualTo(expected));
    }

    [Test]
    public void HostAway_WithAPlayerConnected_IsNotEmpty()
    {
        _room.Join("Anna");

        _room.DisconnectHost(Now);

        Assert.That(_room.EmptySince, Is.Null);
        Assert.That(_room.IsAbandoned(Now.AddHours(2)), Is.False);
    }

    [Test]
    public void LastPlayerDisconnecting_WithHostAway_StartsTheClock()
    {
        var anna = _room.Join("Anna").Value;
        _room.DisconnectHost(Now);

        _room.Disconnect(anna.Id, Now.AddMinutes(5));

        Assert.That(_room.EmptySince, Is.EqualTo(Now.AddMinutes(5)));
    }

    [Test]
    public void LastPlayerLeaving_WithHostAway_StartsTheClock()
    {
        var anna = _room.Join("Anna").Value;
        _room.DisconnectHost(Now);

        _room.Leave(anna.Token, Now.AddMinutes(5));

        Assert.That(_room.EmptySince, Is.EqualTo(Now.AddMinutes(5)));
    }

    [Test]
    public void SomeoneComingBack_StopsTheClock()
    {
        var anna = _room.Join("Anna").Value;
        _room.DisconnectHost(Now);
        _room.Disconnect(anna.Id, Now);

        _room.Rejoin(anna.Token);

        Assert.That(_room.EmptySince, Is.Null);
    }

    [Test]
    public void HostResuming_StopsTheClock()
    {
        _room.DisconnectHost(Now);

        _room.ResumeHost(_room.HostToken);

        Assert.That(_room.EmptySince, Is.Null);
    }

    [Test]
    public void AlreadyEmptyRoom_KeepsItsStartTime()
    {
        var anna = _room.Join("Anna").Value;
        _room.DisconnectHost(Now);
        _room.Disconnect(anna.Id, Now);

        // En sen disconnect för någon som redan var borta ska inte flytta fram klockan.
        _room.Disconnect(anna.Id, Now.AddMinutes(10));

        Assert.That(_room.EmptySince, Is.EqualTo(Now));
    }
}
