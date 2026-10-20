using Mixtape.Domain.Common;

namespace Mixtape.Application.Rooms;

public sealed class RemovePlayerHandler(IGameRoomRepository rooms)
{
    public Task<Result<PlayerRemoved>> HandleAsync(HostSession host, string? playerId) =>
        rooms.WithRoomAsync<PlayerRemoved>(host.RoomCode, room =>
        {
            var removed = room.RemovePlayer(host.HostToken, playerId);
            return removed.IsSuccess ? new PlayerRemoved(removed.Value.Id, LobbyState.From(room)) : removed.Error!;
        });
}
