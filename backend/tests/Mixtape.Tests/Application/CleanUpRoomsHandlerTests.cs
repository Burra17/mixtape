using Mixtape.Application.Connections;
using Mixtape.Application.Rooms;
using Mixtape.Domain.Common;
using Mixtape.Domain.Rooms;
using Mixtape.Infrastructure.Connections;
using Mixtape.Infrastructure.Rooms;

namespace Mixtape.Tests.Application;

public sealed class CleanUpRoomsHandlerTests
{
    private FakeTimeProvider _time = null!;
    private InMemoryGameRoomRepository _rooms = null!;
    private InMemoryConnectionRegistry _connections = null!;
    private CreateRoomHandler _create = null!;
    private DisconnectHandler _disconnect = null!;
    private CleanUpRoomsHandler _cleanUp = null!;
    private RejoinRoomHandler _rejoin = null!;

    [SetUp]
    public void SetUp()
    {
        _time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 20, 20, 0, 0, TimeSpan.Zero));
        _rooms = new InMemoryGameRoomRepository();
        _connections = new InMemoryConnectionRegistry();
        _create = new CreateRoomHandler(_rooms, _connections);
        _disconnect = new DisconnectHandler(_rooms, _connections, _time);
        _cleanUp = new CleanUpRoomsHandler(_rooms, _time);
        _rejoin = new RejoinRoomHandler(_rooms, _connections);
    }

    [Test]
    public async Task EmptyRoom_IsRemovedAfterThirtyMinutesButNotAfterTwentyNine()
    {
        var host = (await _create.HandleAsync(null, "host")).Value.Session;
        await DisconnectAsync("host");

        _time.Advance(TimeSpan.FromMinutes(29));
        var after29 = await _cleanUp.HandleAsync();
        _time.Advance(TimeSpan.FromMinutes(1));
        var after30 = await _cleanUp.HandleAsync();

        Assert.That(after29, Is.Zero);
        Assert.That(after30, Is.EqualTo(1));
        Assert.That(await RoomExists(host.RoomCode), Is.False);
    }

    [Test]
    public async Task RoomWithSomeoneConnected_IsKept()
    {
        var host = (await _create.HandleAsync(null, "host")).Value.Session;

        _time.Advance(TimeSpan.FromHours(3));
        var removed = await _cleanUp.HandleAsync();

        Assert.That(removed, Is.Zero);
        Assert.That(await RoomExists(host.RoomCode), Is.True);
    }

    [Test]
    public async Task PlayerComingBackBeforeThirtyMinutes_KeepsTheRoom()
    {
        var host = (await _create.HandleAsync(null, "host")).Value.Session;
        var anna = (await new JoinRoomHandler(_rooms, _connections).HandleAsync(host.RoomCode, "Anna", "anna-1")).Value.Session;
        await DisconnectAsync("host");
        await DisconnectAsync("anna-1");

        _time.Advance(TimeSpan.FromMinutes(20));
        await _rejoin.HandleAsync(anna, "anna-2");
        _time.Advance(TimeSpan.FromMinutes(20));

        Assert.That(await _cleanUp.HandleAsync(), Is.Zero);
    }

    [Test]
    public async Task OnlyAbandonedRoomsAreRemoved()
    {
        var empty = (await _create.HandleAsync(null, "host-1")).Value.Session;
        var busy = (await _create.HandleAsync(null, "host-2")).Value.Session;
        await DisconnectAsync("host-1");

        _time.Advance(TimeSpan.FromMinutes(30));
        await _cleanUp.HandleAsync();

        Assert.That(await RoomExists(empty.RoomCode), Is.False);
        Assert.That(await RoomExists(busy.RoomCode), Is.True);
    }

    private Task DisconnectAsync(string connectionId) => _disconnect.HandleAsync(_connections.Unregister(connectionId)!);

    private async Task<bool> RoomExists(string code) =>
        (await _rooms.WithRoomAsync(RoomCode.Parse(code)!, _ => Result<bool>.Success(true))).IsSuccess;
}
