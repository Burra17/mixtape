using Mixtape.Domain.Common;
using Mixtape.Domain.Rooms;

namespace Mixtape.Application.Rooms;

public sealed class CreateRoomHandler(IGameRoomRepository rooms)
{
    // previous är värdens gamla rum vid "Nytt spel". Det tas bort bara om token stämmer.
    public async Task<Result<HostRoom>> HandleAsync(HostSession? previous)
    {
        if (previous is not null && RoomCode.Parse(previous.RoomCode) is { } oldCode)
        {
            await rooms.TryRemoveAsync(oldCode, room => room.IsHost(previous.HostToken));
        }

        // Koden slumpas tills den är ledig. Med 19⁴ möjliga koder och några få rum tar det nästan alltid ett försök.
        GameRoom room;
        do
        {
            room = GameRoom.Create(RoomCode.Generate(Random.Shared));
        }
        while (!rooms.TryAdd(room));

        return new HostRoom(new HostSession(room.Code.Value, room.HostToken), LobbyState.From(room));
    }
}
