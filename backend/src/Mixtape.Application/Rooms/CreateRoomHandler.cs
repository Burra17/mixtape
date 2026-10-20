using Mixtape.Application.Connections;
using Mixtape.Domain.Common;
using Mixtape.Domain.Rooms;

namespace Mixtape.Application.Rooms;

public sealed class CreateRoomHandler(IGameRoomRepository rooms, IConnectionRegistry connections)
{
    // previous är värdens gamla rum vid "Nytt spel". Det tas bort bara om token stämmer.
    public async Task<Result<CreatedRoom>> HandleAsync(HostSession? previous, string connectionId)
    {
        string? closedRoomCode = null;
        if (previous is not null && RoomCode.Parse(previous.RoomCode) is { } oldCode
            && await rooms.TryRemoveAsync(oldCode, room => room.IsHost(previous.HostToken)))
        {
            closedRoomCode = oldCode.Value;
        }

        // Koden slumpas tills den är ledig. Med 19⁴ möjliga koder och några få rum tar det nästan alltid ett försök.
        GameRoom room;
        do
        {
            room = GameRoom.Create(RoomCode.Generate(Random.Shared));
        }
        while (!rooms.TryAdd(room));

        connections.Register(connectionId, ConnectionOwner.Host(room.Code.Value));
        return new CreatedRoom(HostSession.For(room), LobbyState.From(room), closedRoomCode);
    }
}
