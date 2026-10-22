using Mixtape.Application.Rooms;
using Mixtape.Domain.Rooms;
using Mixtape.Infrastructure.Connections;
using Mixtape.Infrastructure.Rooms;

namespace Mixtape.Tests.Application;

public sealed class LobbyHandlersTests
{
    private CreateRoomHandler _create = null!;
    private JoinRoomHandler _join = null!;
    private LeaveRoomHandler _leave = null!;
    private RemovePlayerHandler _remove = null!;
    private ResumeRoomHandler _resume = null!;
    private HostSession _host = null!;

    [SetUp]
    public async Task SetUp()
    {
        var rooms = new InMemoryGameRoomRepository();
        var connections = new InMemoryConnectionRegistry();
        _create = new CreateRoomHandler(rooms, connections, TimeProvider.System);
        _join = new JoinRoomHandler(rooms, connections, TimeProvider.System);
        _leave = new LeaveRoomHandler(rooms, TimeProvider.System);
        _remove = new RemovePlayerHandler(rooms, TimeProvider.System);
        _resume = new ResumeRoomHandler(rooms, connections, TimeProvider.System);
        _host = (await _create.HandleAsync(null, "host")).Value.Session;
    }

    [Test]
    public async Task Join_ReturnsSessionAndLobbyWithoutTokens()
    {
        var result = (await _join.HandleAsync(_host.RoomCode.ToLowerInvariant(), "Anna", "anna")).Value;

        Assert.That(result.Session.RoomCode, Is.EqualTo(_host.RoomCode));
        Assert.That(result.State.Players, Is.EqualTo(new[] { new GamePlayer(result.Session.PlayerId, "Anna", 1, true) }));
        Assert.That(result.State.MaxPlayers, Is.EqualTo(GameRoom.MaxPlayers));
        Assert.That(result.State.CanStart, Is.False);
    }

    [TestCase("BCDF")]
    [TestCase("inte en kod")]
    public async Task Join_UnknownRoom_ReturnsNotFound(string roomCode)
    {
        var result = await _join.HandleAsync(roomCode, "Anna", "anna");

        Assert.That(result.Error, Is.EqualTo(RoomErrors.NotFound));
    }

    [Test]
    public async Task Leave_RecognisesThePlayerByTokenNotById()
    {
        var anna = (await _join.HandleAsync(_host.RoomCode, "Anna", "anna")).Value.Session;
        var bertil = (await _join.HandleAsync(_host.RoomCode, "Bertil", "bertil")).Value.Session;

        var result = (await _leave.HandleAsync(anna with { PlayerId = bertil.PlayerId })).Value;

        Assert.That(result.PlayerId, Is.EqualTo(anna.PlayerId));
        Assert.That(result.State.Players.Select(player => player.Name), Is.EqualTo(new[] { "Bertil" }));
    }

    [Test]
    public async Task RemovePlayer_AsHost_ReturnsLobbyWithoutThePlayer()
    {
        var anna = (await _join.HandleAsync(_host.RoomCode, "Anna", "anna")).Value.Session;

        var result = (await _remove.HandleAsync(_host, anna.PlayerId)).Value;

        Assert.That(result.PlayerId, Is.EqualTo(anna.PlayerId));
        Assert.That(result.State.Players, Is.Empty);
    }

    [Test]
    public async Task Resume_ReturnsTheCurrentLobby()
    {
        await _join.HandleAsync(_host.RoomCode, "Anna", "anna");

        var result = (await _resume.HandleAsync(_host, "host")).Value;

        Assert.That(result.State.Players.Select(player => player.Name), Is.EqualTo(new[] { "Anna" }));
    }
}
