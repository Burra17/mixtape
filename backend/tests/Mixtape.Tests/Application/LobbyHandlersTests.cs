using Mixtape.Application.Rooms;
using Mixtape.Domain.Rooms;
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
        _create = new CreateRoomHandler(rooms);
        _join = new JoinRoomHandler(rooms);
        _leave = new LeaveRoomHandler(rooms);
        _remove = new RemovePlayerHandler(rooms);
        _resume = new ResumeRoomHandler(rooms);
        _host = (await _create.HandleAsync(null)).Value.Session;
    }

    [Test]
    public async Task Join_ReturnsSessionAndLobbyWithoutTokens()
    {
        var result = (await _join.HandleAsync(_host.RoomCode.ToLowerInvariant(), "Anna")).Value;

        Assert.That(result.Session.RoomCode, Is.EqualTo(_host.RoomCode));
        Assert.That(result.Lobby.Players, Is.EqualTo(new[] { new LobbyPlayer(result.Session.PlayerId, "Anna", 1) }));
        Assert.That(result.Lobby.MaxPlayers, Is.EqualTo(GameRoom.MaxPlayers));
        Assert.That(result.Lobby.CanStart, Is.False);
    }

    [TestCase("BCDF")]
    [TestCase("inte en kod")]
    public async Task Join_UnknownRoom_ReturnsNotFound(string roomCode)
    {
        var result = await _join.HandleAsync(roomCode, "Anna");

        Assert.That(result.Error, Is.EqualTo(RoomErrors.NotFound));
    }

    [Test]
    public async Task Leave_RecognisesThePlayerByTokenNotById()
    {
        var anna = (await _join.HandleAsync(_host.RoomCode, "Anna")).Value.Session;
        var bertil = (await _join.HandleAsync(_host.RoomCode, "Bertil")).Value.Session;

        var result = (await _leave.HandleAsync(anna with { PlayerId = bertil.PlayerId })).Value;

        Assert.That(result.PlayerId, Is.EqualTo(anna.PlayerId));
        Assert.That(result.Lobby.Players.Select(player => player.Name), Is.EqualTo(new[] { "Bertil" }));
    }

    [Test]
    public async Task RemovePlayer_AsHost_ReturnsLobbyWithoutThePlayer()
    {
        var anna = (await _join.HandleAsync(_host.RoomCode, "Anna")).Value.Session;

        var result = (await _remove.HandleAsync(_host, anna.PlayerId)).Value;

        Assert.That(result.PlayerId, Is.EqualTo(anna.PlayerId));
        Assert.That(result.Lobby.Players, Is.Empty);
    }

    [Test]
    public async Task Resume_ReturnsTheCurrentLobby()
    {
        await _join.HandleAsync(_host.RoomCode, "Anna");

        var result = (await _resume.HandleAsync(_host)).Value;

        Assert.That(result.Lobby.Players.Select(player => player.Name), Is.EqualTo(new[] { "Anna" }));
    }
}
