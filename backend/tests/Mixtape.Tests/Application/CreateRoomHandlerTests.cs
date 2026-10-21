using Mixtape.Application.Rooms;
using Mixtape.Domain.Common;
using Mixtape.Domain.Rooms;
using Mixtape.Infrastructure.Connections;
using Mixtape.Infrastructure.Rooms;

namespace Mixtape.Tests.Application;

public sealed class CreateRoomHandlerTests
{
    private InMemoryGameRoomRepository _rooms = null!;
    private CreateRoomHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _rooms = new InMemoryGameRoomRepository();
        _handler = new CreateRoomHandler(_rooms, new InMemoryConnectionRegistry(), TimeProvider.System);
    }

    [Test]
    public async Task Handle_CreatesRoomsWithUniqueCodes()
    {
        var codes = new HashSet<string>();

        for (var i = 0; i < 200; i++)
        {
            var session = (await _handler.HandleAsync(null, "host")).Value.Session;
            Assert.That(codes.Add(session.RoomCode), Is.True, session.RoomCode);
        }
    }

    [Test]
    public async Task Handle_GivesEachRoomItsOwnHostToken()
    {
        var first = (await _handler.HandleAsync(null, "host")).Value.Session;
        var second = (await _handler.HandleAsync(null, "host")).Value.Session;

        Assert.That(first.HostToken, Has.Length.EqualTo(32));
        Assert.That(second.HostToken, Is.Not.EqualTo(first.HostToken));
    }

    [Test]
    public async Task Handle_WithPreviousSession_RemovesTheOldRoom()
    {
        var old = (await _handler.HandleAsync(null, "host")).Value.Session;

        var created = (await _handler.HandleAsync(old, "host")).Value;

        Assert.That(await RoomExists(old.RoomCode), Is.False);
        Assert.That(created.ClosedRoomCode, Is.EqualTo(old.RoomCode));
    }

    [Test]
    public async Task Handle_WithWrongHostToken_KeepsTheOldRoom()
    {
        var old = (await _handler.HandleAsync(null, "host")).Value.Session;

        var created = (await _handler.HandleAsync(old with { HostToken = "fel" }, "host")).Value;

        Assert.That(await RoomExists(old.RoomCode), Is.True);
        Assert.That(created.ClosedRoomCode, Is.Null);
    }

    private async Task<bool> RoomExists(string code) =>
        (await _rooms.WithRoomAsync(RoomCode.Parse(code)!, _ => Result<bool>.Success(true))).IsSuccess;
}
