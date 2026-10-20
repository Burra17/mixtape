using Mixtape.Application.Rooms;
using Mixtape.Domain.Rooms;
using Mixtape.Infrastructure.Rooms;

namespace Mixtape.Tests.Application;

public sealed class ResumeRoomHandlerTests
{
    private CreateRoomHandler _create = null!;
    private ResumeRoomHandler _resume = null!;

    [SetUp]
    public void SetUp()
    {
        var rooms = new InMemoryGameRoomRepository();
        _create = new CreateRoomHandler(rooms);
        _resume = new ResumeRoomHandler(rooms);
    }

    [Test]
    public async Task Handle_WithRightHostToken_ReturnsTheRoom()
    {
        var session = (await _create.HandleAsync(null)).Value.Session;

        var result = await _resume.HandleAsync(session);

        Assert.That(result.Value.Session, Is.EqualTo(session));
    }

    [Test]
    public async Task Handle_WithWrongHostToken_ReturnsNotFound()
    {
        var session = (await _create.HandleAsync(null)).Value.Session;

        var result = await _resume.HandleAsync(session with { HostToken = "fel" });

        Assert.That(result.Error, Is.EqualTo(RoomErrors.NotFound));
    }

    [TestCase("BCDF")]
    [TestCase("inte en kod")]
    public async Task Handle_ForUnknownRoom_ReturnsNotFound(string roomCode)
    {
        var result = await _resume.HandleAsync(new HostSession(roomCode, "token"));

        Assert.That(result.Error, Is.EqualTo(RoomErrors.NotFound));
    }
}
