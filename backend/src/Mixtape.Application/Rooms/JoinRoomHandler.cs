using Mixtape.Domain.Common;

namespace Mixtape.Application.Rooms;

public sealed class JoinRoomHandler(IGameRoomRepository rooms)
{
    public Task<Result<JoinedRoom>> HandleAsync(string? roomCode, string? name) =>
        rooms.WithRoomAsync<JoinedRoom>(roomCode, room =>
        {
            var joined = room.Join(name);
            if (!joined.IsSuccess)
            {
                return joined.Error!;
            }

            var player = joined.Value;
            return new JoinedRoom(new PlayerSession(room.Code.Value, player.Id, player.Token), LobbyState.From(room));
        });
}
