using Mixtape.Domain.Common;
using Mixtape.Domain.Rooms;

namespace Mixtape.Application.Rooms;

public sealed class RemovePlayerHandler(IGameRoomRepository rooms)
{
    public async Task<Result<PlayerRemoved>> HandleAsync(HostSession host, string? playerId)
    {
        if (RoomCode.Parse(host.RoomCode) is not { } code)
        {
            return RoomErrors.NotFound;
        }

        return await rooms.WithRoomAsync<PlayerRemoved>(code, room =>
        {
            var removed = room.RemovePlayer(host.HostToken, playerId);
            return removed.IsSuccess ? new PlayerRemoved(removed.Value.Id, LobbyState.From(room)) : removed.Error!;
        });
    }
}
