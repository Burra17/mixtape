using Mixtape.Application.Connections;
using Mixtape.Application.Rooms;
using Mixtape.Domain.Common;
using Mixtape.Domain.Rooms;
using Mixtape.Infrastructure.Connections;
using Mixtape.Infrastructure.Rooms;

namespace Mixtape.Tests.Application;

public sealed class PresenceHandlersTests
{
    private InMemoryGameRoomRepository _rooms = null!;
    private InMemoryConnectionRegistry _connections = null!;
    private JoinRoomHandler _join = null!;
    private RejoinRoomHandler _rejoin = null!;
    private DisconnectHandler _disconnect = null!;
    private HostSession _host = null!;

    [SetUp]
    public async Task SetUp()
    {
        _rooms = new InMemoryGameRoomRepository();
        _connections = new InMemoryConnectionRegistry();
        _join = new JoinRoomHandler(_rooms, _connections);
        _rejoin = new RejoinRoomHandler(_rooms, _connections);
        _disconnect = new DisconnectHandler(_rooms, _connections);
        _host = (await new CreateRoomHandler(_rooms, _connections).HandleAsync(null, "host")).Value.Session;
    }

    [Test]
    public async Task Disconnect_LastConnection_MarksThePlayerAway()
    {
        var anna = (await _join.HandleAsync(_host.RoomCode, "Anna", "anna-1")).Value.Session;

        var lobby = (await DisconnectAsync("anna-1")).Value;

        Assert.That(lobby.Players.Single(player => player.Id == anna.PlayerId).IsConnected, Is.False);
    }

    [Test]
    public async Task Disconnect_AfterRejoinOnNewConnection_KeepsThePlayerConnected()
    {
        var anna = (await _join.HandleAsync(_host.RoomCode, "Anna", "anna-1")).Value.Session;

        // Mobilen låses upp och återansluter innan den gamla anslutningens disconnect har kommit fram.
        await _rejoin.HandleAsync(anna, "anna-2");
        var lobby = (await DisconnectAsync("anna-1")).Value;

        Assert.That(lobby.Players.Single().IsConnected, Is.True);
    }

    [Test]
    public async Task Rejoin_AfterDisconnect_ReturnsSessionAndConnectedLobby()
    {
        var anna = (await _join.HandleAsync(_host.RoomCode, "Anna", "anna-1")).Value.Session;
        await DisconnectAsync("anna-1");

        var result = (await _rejoin.HandleAsync(anna with { RoomCode = anna.RoomCode.ToLowerInvariant() }, "anna-2")).Value;

        Assert.That(result.Session, Is.EqualTo(anna));
        Assert.That(result.Lobby.Players.Single().IsConnected, Is.True);
    }

    [Test]
    public async Task Rejoin_WithWrongToken_ReturnsPlayerNotFoundAndRegistersNothing()
    {
        var anna = (await _join.HandleAsync(_host.RoomCode, "Anna", "anna-1")).Value.Session;

        var result = await _rejoin.HandleAsync(anna with { PlayerToken = "fel" }, "inkräktare");

        Assert.That(result.Error, Is.EqualTo(PlayerErrors.NotFound));
        Assert.That(_connections.Unregister("inkräktare"), Is.Null);
    }

    [Test]
    public async Task Rejoin_ToMissingRoom_ReturnsRoomNotFound()
    {
        var result = await _rejoin.HandleAsync(new PlayerSession("BCDF", "id", "token"), "anna-1");

        Assert.That(result.Error, Is.EqualTo(RoomErrors.NotFound));
    }

    [Test]
    public async Task Disconnect_Host_PausesTheRoom()
    {
        var lobby = (await DisconnectAsync("host")).Value;

        Assert.That(lobby.IsPaused, Is.True);
    }

    [Test]
    public async Task CheckRoom_KeepsThePauseUntilTheHostResumes()
    {
        await DisconnectAsync("host");

        var checkedRoom = await new CheckRoomHandler(_rooms).HandleAsync(_host);
        var stillPaused = (await _rooms.WithRoomAsync(RoomCode.Parse(_host.RoomCode)!, room => Result<bool>.Success(room.IsPaused))).Value;
        var resumed = (await new ResumeRoomHandler(_rooms, _connections).HandleAsync(_host, "host-2")).Value;

        Assert.That(checkedRoom.Value, Is.EqualTo(_host));
        Assert.That(stillPaused, Is.True);
        Assert.That(resumed.Lobby.IsPaused, Is.False);
    }

    // Som hubben: anslutningen tas bort ur registret före handlern.
    private Task<Result<LobbyState>> DisconnectAsync(string connectionId) =>
        _disconnect.HandleAsync(_connections.Unregister(connectionId)!);
}
